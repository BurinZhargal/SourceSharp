using System;
using System.Collections.Generic;

namespace sourcesharp.app.legion
{
    internal class InputManager
    {
        // Singleton экземпляр и глобальный доступ как в C++ (extern CInputManager *g_pInputManager)
        private static readonly InputManager _instance = new();
        public static InputManager Instance => _instance;

        // Эмуляция внутренних подсистем движка (замени на свои реальные классы/интерфейсы)
        private readonly Dictionary<string, string> _keyBindings = new();
        private readonly HashSet<int> _buttonUpToEngine = new();
        private readonly Queue<string> _commandBuffer = new();

        public bool Init()
        {
            // FIXME: Читать биндинги клавиш из файла
            SetBinding("w", "+forward");
            SetBinding("s", "+back");
            SetBinding("`", "toggleconsole");
            
            _buttonUpToEngine.Clear();
            return true;
        }

        public void AddCommand(string command)
        {
            _commandBuffer.Enqueue(command);
        }

        public void ProcessCommands()
        {
            // Извлекаем команды из буфера
            while (_commandBuffer.TryDequeue(out string? fullCommand))
            {
                if (string.IsNullOrWhiteSpace(fullCommand)) continue;

                // Парсим команду на имя и аргументы (упрощенный аналог CCommand)
                string[] args = fullCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (args.Length == 0) continue;

                string commandName = args[0];

                // Ищем команду (Аналог FindNamedCommand)
                var pCommand = CVarSystem.FindNamedCommand(commandName);
                if (pCommand != null && pCommand.IsCommand)
                {
                    pCommand.Dispatch(args);
                    continue;
                }

                // Ищем переменную (Аналог g_pCVar->FindVar)
                var pConVar = CVarSystem.FindVar(commandName);
                if (pConVar == null) continue;

                // Если только имя переменной — выводим её описание
                if (args.Length == 1)
                {
                    PrintConCommandBaseDescription(pConVar);
                    continue;
                }

                // Извлекаем оставшуюся строку аргументов и очищаем от кавычек
                string remaining = fullCommand[commandName.Length..].Trim();
                if (remaining.StartsWith('"') && remaining.EndsWith('"') && remaining.Length > 1)
                {
                    remaining = remaining[1..^1].Trim();
                }

                if (pConVar.IsBitSet(ConVarFlags.FCVAR_NEVER_AS_STRING))
                {
                    if (float.TryParse(remaining, out float floatVal))
                        pConVar.SetValue(floatVal);
                }
                else
                {
                    pConVar.SetValue(remaining);
                }
            }
        }

        public void Update()
        {
            InputSystem.PollInputState();
            var events = InputSystem.GetEventData();

            foreach (var ev in events)
            {
                if (ev.Type == InputEventType.IE_Quit)
                {
                    GameManager.Stop();
                    break;
                }

                bool bypassVGui = false;
                switch (ev.Type)
                {
                    case InputEventType.IE_AppActivated:
                        if (ev.Data == 0)
                        {
                            _buttonUpToEngine.Clear();
                        }
                        break;

                    case InputEventType.IE_ButtonReleased:
                        int releaseCode = ev.Data;
                        if (_buttonUpToEngine.Contains(releaseCode))
                        {
                            _buttonUpToEngine.Remove(releaseCode);
                            bypassVGui = true;
                        }
                        break;
                }

                if (!bypassVGui)
                {
                    if (UIManager.ProcessInputEvent(ev))
                        continue;
                }

                bool bButtonDown = ev.Type == InputEventType.IE_ButtonPressed;
                bool bButtonUp = ev.Type == InputEventType.IE_ButtonReleased;

                if (bButtonDown || bButtonUp)
                {
                    int code = ev.Data;
                    if (bButtonDown)
                    {
                        _buttonUpToEngine.Add(code);
                    }

                    if (!_keyBindings.TryGetValue(code.ToString(), out string? pBinding)) 
                        continue;

                    if (!pBinding.StartsWith('+'))
                    {
                        if (bButtonDown)
                        {
                            AddCommand(pBinding);
                        }
                        continue;
                    }

                    // Используем удобную интерполяцию строк .NET 10 вместо Q_snprintf
                    char prefix = bButtonUp ? '-' : '+';
                    string cmd = $"{prefix}{pBinding[1..]} {code}\n";
                    AddCommand(cmd);
                }
            }

            ProcessCommands();
        }

        public void PrintConCommandBaseDescription(ConVar var)
        {
            if (!var.IsCommand)
            {
                string value = var.IsBitSet(ConVarFlags.FCVAR_NEVER_AS_STRING) 
                    ? var.GetFloatValue().ToString("F6") 
                    : var.GetStringValue();

                Console.Write($"\"{var.Name}\" = \"{value}\"");

                if (!string.Equals(value, var.DefaultValue, StringComparison.OrdinalIgnoreCase))
                {
                    Console.Write($" ( def. \"{var.DefaultValue}\" )");
                }

                if (var.HasMin) Console.Write($" min. {var.MinValue}");
                if (var.HasMax) Console.Write($" max. {var.MaxValue}");
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine($"\"{var.Name}\"");
            }

            if (!string.IsNullOrEmpty(var.HelpText))
            {
                Console.WriteLine($" - {var.HelpText}");
            }
        }

        private void SetBinding(string key, string command) => _keyBindings[key] = command;
    }

    // --- Заглушки для компиляции (замени своими реальными классами движка SourceSharp) ---
    internal enum InputEventType { IE_Quit, IE_AppActivated, IE_ButtonPressed, IE_ButtonReleased }
    internal enum ConVarFlags { FCVAR_NEVER_AS_STRING }
    internal record InputEvent(InputEventType Type, int Data);
    internal class ConVar { 
        public string Name => "dummy"; public bool IsCommand => false; public string DefaultValue => ""; 
        public bool HasMin => false; public float MinValue => 0; public bool HasMax => false; public float MaxValue => 0;
        public string HelpText => ""; public bool IsBitSet(ConVarFlags f) => false;
        public float GetFloatValue() => 0; public string GetStringValue() => "";
        public void SetValue(float v) {} public void SetValue(string s) {} public void Dispatch(string[] args) {}
    }
    static class CVarSystem { public static ConVar? FindNamedCommand(string n) => null; public static ConVar? FindVar(string n) => null; }
    static class InputSystem { public static void PollInputState() {} public static List<InputEvent> GetEventData() => new(); }
    static class UIManager { public static bool ProcessInputEvent(InputEvent e) => false; }
    static class GameManager { public static void Stop() {} }
}
