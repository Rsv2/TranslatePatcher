using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Resources;

/// <summary>
/// View-model class
/// </summary>
public class ViewModel
{
    /// <summary>
    /// Кнопка установки русификатора.
    /// </summary>
    private RelayCommand translate;
    /// <summary>
    /// Кнопка установки русификатора.
    /// </summary>
    public RelayCommand Translate => translate ?? (translate = new RelayCommand(obj => OpenFile()));

    /// <summary>
    /// Окно патчера.
    /// </summary>
    private Window Win { get; set; }
    /// <summary>
    /// Пути к выбранным файлам.
    /// </summary>
    public static List<string> Paths { get; set; } = new List<string>();
    /// <summary>
    /// Первые 12 байт архива.
    /// </summary>
    public static byte[] StartCodeTable { get; set; }
    /// <summary>
    /// Промежуточная после компоновки архива строка из 16 байт.
    /// Происхождение и назначение неизвестно, при распаковке архива не используется.
    /// Зато приведёт к ошибке запакованной игры, если её выбросить.
    /// </summary>
    public static byte[] MidCodeTable { get; set; }
    /// <summary>
    /// Текст Game.ini
    /// </summary>
    private const string Ini = "[Game]\r\n" +
            "Library=System\\RGSS301.dll\r\n" +
            "Scripts=Data\\Scripts.rvdata2\r\n" +
            "Title=(Название игры)\r\n" +
            "RTP=RPGVXAce\r\n" +
            "Description=(no description)\r\n";
    /// <summary>
    /// Название и версия русификатора для вывода в верхней строке.
    /// </summary>
    public string Version {  get; set; }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="win">Текущее окно</param>
    public ViewModel(Window win)
    {
        Version = $"{Assembly.GetExecutingAssembly().GetName().Name} v. {Assembly.GetExecutingAssembly().GetName().Version}";
        StartCodeTable = new byte[] {  };
        MidCodeTable = new byte[] {  };
        Win = win;
    }
    /// <summary>
    /// Выбор файла.
    /// </summary>
    private void OpenFile()
    {
        OpenFileDialog OFD = new OpenFileDialog()
        {
            Multiselect = false,
            Filter = "RPG Maker Archives (Game.rgss3a)|Game.rgss3a",
            //Message - Specify the path to the file.
            Title = "Указать путь к файлу Game.rgss3a"
        };
        if (OFD.ShowDialog() == true)
        {
            string output = OFD.FileName.Substring(0, OFD.FileName.LastIndexOf("\\") + 1);
            if (OFD.FileName.ToLower().EndsWith(".rgss3a"))
            {
                byte[] test = File.ReadAllBytes(OFD.FileName);
                // Проверка на принадлежность архива игре по 3 байтам уникального ключа.
                // Check required game archive with 3 key bytes.
                if (test[7] == 0x03 && test[8] == 0x8B && test[9] == 0x7B)
                {
                    Unpacker(OFD.FileName, output);
                    File.WriteAllText($"{output}Game.ini", Ini, Encoding.Unicode);
                    if (Paths.Contains("Game.exe")) SaveGameExe(output);
                    // Сообщение о завершении перевода.
                    // End message.
                    MessageBox.Show("Готово!");
                    Win.Close();
                }
                else
                {
                    // Сообщение об ошибке, типа - "Требуется архивированная версия игры\r\n\"(Название игры)\" v. (версия)"
                    // Error message, something like - "Required packed type of game, version... etc."
                    MessageBox.Show("Требуется архивированная версия игры\r\n\"Корпорация Справедливость\" v. 1.04");
                }
            }
        }
    }
    /// <summary>
    /// Сохранение своего Game.exe
    /// </summary>
    /// <param name="output">Путь к папке игры.</param>
    private void SaveGameExe(string output)
    {
        StreamResourceInfo res = Application.GetResourceStream(new Uri("Game\\Game.exe", UriKind.Relative));
        res.Stream.CopyTo(new FileStream($"{output}{"Game.exe"}", FileMode.OpenOrCreate, FileAccess.ReadWrite));
        res.Stream.Close();
    }
    /// <summary>
    /// Кнопка установки русификатора.
    /// </summary>
    private void Unpacker(string file, string Output)
    {
        RGSSADv3 rGSSAD = new RGSSADv3(file);
    }
}