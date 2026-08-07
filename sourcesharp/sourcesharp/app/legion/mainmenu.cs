using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace sourcesharp.app.legion;

internal class MainMenu : BaseMenu
{
    private readonly List<string> _mapResources = new();

    public MainMenu(Panel parent, string panelName) : base(parent, panelName)
    {
        // Инициализация вызовет асинхронные методы в фоновом режиме
        string layoutPath = "resource/mainmenu.res";
        //string resourcePath = "maps/*.res";

        if (File.Exists(layoutPath)) LoadMenuLayout(layoutPath);
        if (File.Exists(resourcePath)) ParseResourceList(resourcePath);
    }

    // Асинхронный разбор строк макета меню в духе Dart Future
    async private void LoadMenuLayout(string path)
    {
        try
        {
            // Неблокирующее чтение файла «в лоб»
            string[] lines = await File.ReadAllLinesAsync(path);
            
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                string trimmedLine = line.Trim();
                // Логика разбора строк .res (поиск кнопок JoinGame, Quit и т.д.)
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainMenu] Ошибка асинхронной загрузки макета: {ex.Message}");
        }
    }

    // Асинхронный терминальный разбор ресурсного пакета карты
    async private void ParseResourceList(string path)
    {
        try
        {
            // Читаем все строки ресурсного пакета асинхронно
            string[] lines = await File.ReadAllLinesAsync(path);

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("//")) 
                    continue;

                string resourcePath = trimmedLine.Replace("\"", "").Trim();

                if (!string.IsNullOrEmpty(resourcePath))
                {
                    _mapResources.Add(resourcePath);
                    Console.WriteLine($"[Terminal-Parser] Добавлен ресурс к загрузке: {resourcePath}");
                    
                    // Позже здесь можно будет запустить асинхронную загрузку ассета в DX12:
                    // await LoadAssetToDX12Async(resourcePath);
                }
            }
            Console.WriteLine($"[Terminal-Parser] Всего зарегистрировано loose-файлов для BSP: {_mapResources.Count} шт.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Terminal-Parser] Ошибка парсинга ресурсов: {ex.Message}");
        }
    }
}
