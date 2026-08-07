using System;
using System.Collections.Generic;

namespace sourcesharp.app.legion;

internal class MenuManager : IGameSystem
{
    // Одиночка (Singleton)
    private static readonly MenuManager _instance = new();
    public static MenuManager Instance => _instance;

    // Вместо кастомных фабрик Valve используем встроенные делегаты Func<Panel, Panel>
    private readonly Dictionary<string, Func<Panel, Panel>> _menuFactories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<Panel> _activeMenus = new();

    // Переменные состояния фрейма
    private bool _popRequested;
    private bool _popAllRequested;
    private string? _pushRequestedMenuName;

    private MenuManager() { }

    /// <summary>
    /// Регистрация меню в духе вашего Terminal-подхода: связываем имя с функцией создания
    /// </summary>
    public void RegisterMenu(string menuName, Func<Panel, Panel> factoryFunc)
    {
        _menuFactories[menuName] = factoryFunc;
    }

    public bool Init()
    {
        // Инициализируем дефолтные меню (пример динамической регистрации вместо REGISTER_MENU макроса)
        RegisterMenu("MainMenu", (parent) => new MainMenu(parent, "MainMenu"));
        RegisterMenu("JoinGameMenu", (parent) => new JoinGameMenu(parent, "JoinGameMenu"));

        _popRequested = false;
        _popAllRequested = false;
        _pushRequestedMenuName = null;

        return true;
    }

    public void Shutdown()
    {
        CleanUpAllMenus();
    }

    public void PushMenu(string menuName)
    {
        if (_pushRequestedMenuName != null)
        {
            throw new InvalidOperationException("Менеджер меню: Нельзя запросить загрузку двух меню за один фрейм!");
        }

        if (!_menuFactories.ContainsKey(menuName))
        {
            Console.WriteLine($"[MenuManager] Предупреждение: Попытка загрузить неизвестное меню {menuName}");
            return;
        }

        _pushRequestedMenuName = menuName;
    }

    public void PopMenu()
    {
        if (_popRequested)
            throw new InvalidOperationException("Менеджер меню: Нельзя запросить закрытие двух меню за один фрейм!");
        if (_pushRequestedMenuName != null)
            throw new InvalidOperationException("Менеджер меню: Нельзя закрывать меню сразу после запроса на открытие в одном фрейме!");

        _popRequested = true;
    }

    public void PopAllMenus()
    {
        if (_pushRequestedMenuName != null)
            throw new InvalidOperationException("Менеджер меню: Нельзя закрывать все меню сразу после запроса на открытие в одном фрейме!");

        _popAllRequested = true;
    }

    public void SwitchToMenu(string menuName)
    {
        if (_popRequested || _pushRequestedMenuName != null)
            throw new InvalidOperationException("Менеджер меню: Конфликт команд переключения меню внутри одного фрейма!");

        if (!_menuFactories.ContainsKey(menuName))
        {
            Console.WriteLine($"[MenuManager] Предупреждение: Попытка переключения на неизвестное меню {menuName}");
            return;
        }

        _popRequested = true;
        _pushRequestedMenuName = menuName;
    }

    public string? GetTopmostPanelName()
    {
        return _activeMenus.Count == 0 ? null : _activeMenus.Peek().Name;
    }

    /// <summary>
    /// Пофреймовое обновление состояний окон интерфейса
    /// </summary>
    public void Update()
    {
        if (_popAllRequested)
        {
            CleanUpAllMenus();
            _popAllRequested = false;
            return;
        }

        // Обработка закрытия (Pop)
        if (_popRequested)
        {
            if (_activeMenus.Count == 0)
                throw new InvalidOperationException("Менеджер меню: Попытка закрыть меню, когда стек пуст!");

            Panel topMenu = _activeMenus.Pop();
            topMenu.Dispose(); // Вместо MarkForDeletion() в C# используем стандартный Dispose

            if (_activeMenus.Count > 0)
            {
                Panel newTop = _activeMenus.Peek();
                newTop.SetVisible(true);
                newTop.SetParent(UIManager.Instance.GetRootPanel(UserInterfaceRoot.Menu));
            }
            else
            {
                UIManager.Instance.EnablePanel(UserInterfaceRoot.Menu, false);
            }
            _popRequested = false;
        }

        // Обработка открытия (Push)
        if (_pushRequestedMenuName != null)
        {
            if (_activeMenus.Count > 0)
            {
                Panel previousTop = _activeMenus.Peek();
                previousTop.SetVisible(false);
                previousTop.SetParent(null);
            }
            else
            {
                UIManager.Instance.EnablePanel(UserInterfaceRoot.Menu, true);
            }

            // Вызываем делегат-фабрику напрямую по имени
            Panel rootPanel = UIManager.Instance.GetRootPanel(UserInterfaceRoot.Menu);
            Panel newMenu = _menuFactories[_pushRequestedMenuName](rootPanel);
            
            _activeMenus.Push(newMenu);
            
            if (newMenu is Frame frame)
            {
                frame.Activate(); // Активируем окно, если это Frame
            }

            _pushRequestedMenuName = null;
        }
    }

    public void CleanUpAllMenus()
    {
        while (_activeMenus.Count > 0)
        {
            Panel topMenu = _activeMenus.Pop();
            topMenu.Dispose();
        }
        UIManager.Instance.EnablePanel(UserInterfaceRoot.Menu, false);
    }
}

// --- Заглушки типов для корректной сборки ---
internal enum UserInterfaceRoot { Menu }
internal class Frame : Panel { public Frame(Panel p, string n) : base(p, n) { } public void Activate() { } }
