using System.Security.Cryptography;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        try
        {
            string filePath = ChooseFilePath();
            Console.Write("Введіть ключ: ");
            byte[] key = ParseKey(Console.ReadLine());

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Файл за шляхом {filePath} не існує. Створюємо новий зашифрований файл.");
                EncryptAndSave("", filePath, key); // Створення порожнього файлу
            }

            Console.WriteLine($"Шлях до зашифрованого файлу: {filePath}");

            string decryptedText = LoadAndDecrypt(filePath, key);
            Console.WriteLine($"Зміст зашифрованого файлу:\n{decryptedText}");

            Console.WriteLine("\nОберіть опцію:");
            Console.WriteLine("1. Редагувати текст");
            Console.Write("Введіть номер опції: ");

            if (!int.TryParse(Console.ReadLine(), out int choice))
            {
                Console.WriteLine("Некоректний формат опції.");
                return;
            }

            switch (choice)
            {
                case 1:
                    EditTextMenu(filePath, key);
                    break;

                default:
                    Console.WriteLine("Невірний вибір опції.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка: {ex.Message}");
        }
    }

    private static void EditTextMenu(string filePath, byte[] key)
    {
        while (true)
        {
            Console.Clear();
            string decryptedText = LoadAndDecrypt(filePath, key);
            DisplayNumberedLines(decryptedText);
            Console.WriteLine("\nОберіть опцію:");
            Console.WriteLine("1. Заповнити заново");
            Console.WriteLine("2. Додати новий рядок");
            Console.WriteLine("3. Редагувати рядок");
            Console.WriteLine("0. Завершити редагування");
            Console.Write("Введіть номер опції: ");

            if (!int.TryParse(Console.ReadLine(), out int choice))
            {
                Console.WriteLine("Некоректний формат опції. Спробуйте ще раз.");
                continue;
            }

            switch (choice)
            {
                case 1:
                    Console.WriteLine("\nВведіть новий текст для зашифрування (введіть '0' для завершення вводу):");
                    StringBuilder newTextBuilder = new StringBuilder();
                    string newLine;
                    while ((newLine = Console.ReadLine()) != "0")
                    {
                        newTextBuilder.AppendLine(newLine);
                    }
                    string newText = newTextBuilder.ToString();
                    EncryptAndSave(newText, filePath, key);
                    Console.WriteLine("Дані успішно зашифровано та збережено.");
                    break;

                case 2:
                    Console.WriteLine("\nВведіть новий рядок для зашифрування (введіть '0' для завершення вводу):");
                    StringBuilder addLineBuilder = new StringBuilder();
                    string addLine;
                    while ((addLine = Console.ReadLine()) != "0")
                    {
                        addLineBuilder.AppendLine(addLine);
                    }
                    string addText = addLineBuilder.ToString();
                    Console.WriteLine("Введіть номер рядка, після якого додати новий рядок (або '0' для завершення): ");
                    if (int.TryParse(Console.ReadLine(), out int insertIndex) && insertIndex >= 0 && insertIndex <= decryptedText.Split('\n').Length)
                    {
                        string originalText = LoadAndDecrypt(filePath, key);
                        string[] lines = originalText.Split('\n');
                        Array.Resize(ref lines, lines.Length + 1);
                        Array.Copy(lines, insertIndex - 1, lines, insertIndex, lines.Length - insertIndex);
                        lines[insertIndex - 1] = addText;
                        string updatedText = string.Join("\n", lines);
                        EncryptAndSave(updatedText, filePath, key);
                        Console.WriteLine("Дані успішно зашифровано та збережено.");
                    }
                    else
                    {
                        Console.WriteLine("Некоректний номер рядка. Введіть ще раз.");
                    }
                    break;

                case 3:
                    Console.Write("\nВведіть номер рядка для редагування (або '0' для завершення): ");
                    if (int.TryParse(Console.ReadLine(), out int editIndex) && editIndex >= 0 && editIndex <= decryptedText.Split('\n').Length)
                    {
                        string originalText = LoadAndDecrypt(filePath, key);
                        string[] lines = originalText.Split('\n');
                        Console.WriteLine($"Редагуйте рядок {editIndex}:\n{lines[editIndex - 1]}");
                        lines[editIndex - 1] = Console.ReadLine();
                        string updatedText = string.Join("\n", lines);
                        EncryptAndSave(updatedText, filePath, key);
                        Console.WriteLine("Дані успішно зашифровано та збережено.");
                    }
                    else
                    {
                        Console.WriteLine("Некоректний номер рядка. Введіть ще раз.");
                    }
                    break;

                case 0:
                    return;

                default:
                    Console.WriteLine("Невірний вибір опції. Спробуйте ще раз.");
                    break;
            }
        }
    }

    private static void DisplayNumberedLines(string text)
    {
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {lines[i]}");
        }
    }

    private static byte[] ParseKey(string keyInput)
    {
        string[] keyParts = keyInput.Split(',');
        byte[] key = new byte[keyParts.Length];

        for (int i = 0; i < keyParts.Length; i++)
        {
            key[i] = byte.Parse(keyParts[i].Trim().Substring(2), System.Globalization.NumberStyles.HexNumber);
        }

        return key;
    }

    private static void EncryptAndSave(string data, string filePath, byte[] key)
    {
        using (AesManaged aes = new AesManaged())
        {
            aes.Key = key;

            aes.GenerateIV();

            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            using (FileStream fsOutput = new FileStream(filePath, FileMode.Create))
            using (CryptoStream cs = new CryptoStream(fsOutput, encryptor, CryptoStreamMode.Write))
            {
                fsOutput.Write(aes.IV, 0, aes.IV.Length);

                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                cs.Write(dataBytes, 0, dataBytes.Length);
            }
        }
    }

    private static string LoadAndDecrypt(string filePath, byte[] key)
    {
        using (AesManaged aes = new AesManaged())
        {
            aes.Key = key;

            byte[] iv = new byte[16];
            using (FileStream fsInput = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                fsInput.Read(iv, 0, iv.Length);
                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                using (MemoryStream msOutput = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(msOutput, decryptor, CryptoStreamMode.Write))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = fsInput.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        cs.Write(buffer, 0, bytesRead);
                    }

                    cs.FlushFinalBlock();

                    return Encoding.UTF8.GetString(msOutput.ToArray());
                }
            }
        }
    }

    private static string ChooseFilePath()
    {
        Console.WriteLine("\nОберіть опцію для шляху до зашифрованого файлу:");
        Console.WriteLine("1. Ввести власний шлях");
        Console.WriteLine("2. Використати за замовчуванням");
        Console.Write("Ваш вибір: ");

        int choice = int.Parse(Console.ReadLine());

        switch (choice)
        {
            case 1:
                Console.Write("Введіть шлях до зашифрованого файлу: ");
                return Console.ReadLine();

            case 2:
                return "замовчування.dat";

            default:
                Console.WriteLine("Невірний вибір опції. Використано за замовчуванням.");
                return "замовчування.dat";
        }
    }
}
