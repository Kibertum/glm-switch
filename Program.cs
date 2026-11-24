using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Forms;

namespace GLMSwitchApp;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--self-test")
        {
            SettingsSwitcher.RunSelfTest();
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private readonly SettingsSwitcher _switcher = new();
    private readonly ComboBox _keySelector = new();
    private readonly TextBox _keyNameBox = new();
    private readonly TextBox _keyBox = new();
    private readonly Label _status = new();

    public MainForm()
    {
        Text = "GLM Switch 0.1";
        Width = 510;
        Height = 405;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new System.Drawing.Font("Segoe UI", 9F);
        using (var iconStream = typeof(MainForm).Assembly.GetManifestResourceStream("GLMSwitchApp.glm-switch.ico"))
        {
            if (iconStream is not null)
            {
                Icon = new System.Drawing.Icon(iconStream);
            }
        }

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 10
        };
        for (var row = 0; row < 9; row++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(layout);

        layout.Controls.Add(new Label
        {
            Text = "Сохранённые ключи",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
        }, 0, 0);

        _keySelector.Dock = DockStyle.Top;
        _keySelector.DropDownStyle = ComboBoxStyle.DropDownList;
        _keySelector.SelectedIndexChanged += (_, _) =>
        {
            if (_keySelector.SelectedItem is string selectedName)
            {
                _keyNameBox.Text = selectedName;
            }
        };
        layout.Controls.Add(_keySelector, 0, 1);

        layout.Controls.Add(new Label
        {
            Text = "Имя нового ключа",
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 6)
        }, 0, 2);

        _keyNameBox.Dock = DockStyle.Top;
        _keyNameBox.PlaceholderText = "Например, личный или рабочий";
        layout.Controls.Add(_keyNameBox, 0, 3);

        layout.Controls.Add(new Label
        {
            Text = "API-ключ (пусто — использовать выбранный)",
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 6)
        }, 0, 4);

        _keyBox.Dock = DockStyle.Top;
        _keyBox.UseSystemPasswordChar = true;
        _keyBox.PlaceholderText = "Новый ключ сохраняется зашифрованным";
        layout.Controls.Add(_keyBox, 0, 5);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 12, 0, 8)
        };

        var saveButton = new Button { Text = "Сохранить ключ", AutoSize = true };
        saveButton.Click += (_, _) => SaveKey();
        buttons.Controls.Add(saveButton);

        var removeButton = new Button { Text = "Удалить выбранный", AutoSize = true };
        removeButton.Click += (_, _) => RemoveSelectedKey();
        buttons.Controls.Add(removeButton);
        layout.Controls.Add(buttons, 0, 6);

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 2, 0, 8)
        };

        var onButton = new Button { Text = "Переключить на GLM", AutoSize = true };
        onButton.Click += (_, _) => RunAction(() =>
        {
            var selected = _switcher.SwitchToGlm(
                _keySelector.SelectedItem as string, _keyNameBox.Text, _keyBox.Text);
            _keyNameBox.Text = selected;
            ReloadKeyNames(selected);
        },
            "GLM включён. Перезапустите текущий чат Claude Code.");
        actions.Controls.Add(onButton);

        var offButton = new Button { Text = "Вернуть Claude", AutoSize = true };
        offButton.Click += (_, _) => RunAction(
            _switcher.SwitchToClaude,
            "Claude восстановлен. Перезапустите текущий чат Claude Code.");
        actions.Controls.Add(offButton);
        layout.Controls.Add(actions, 0, 7);

        _status.AutoSize = true;
        _status.Margin = new Padding(0, 4, 0, 6);
        layout.Controls.Add(_status, 0, 8);

        layout.Controls.Add(new Label
        {
            Text = "Ключи зашифрованы Windows DPAPI. Пока GLM включён, выбранный ключ также находится в settings.json в открытом виде — Claude Code должен прочитать его оттуда.",
            AutoSize = true,
            MaximumSize = new System.Drawing.Size(455, 0),
            ForeColor = System.Drawing.SystemColors.GrayText,
            Margin = new Padding(0, 4, 0, 0)
        }, 0, 9);

        ReloadKeyNames();
        RefreshStatus();
    }

    private void SaveKey()
    {
        try
        {
            _switcher.SaveKey(_keyNameBox.Text, _keyBox.Text);
            ReloadKeyNames(_keyNameBox.Text.Trim());
            _keyBox.Clear();
            RefreshStatus();
            MessageBox.Show(this, "Ключ сохранён зашифрованным.", "GLM Switch",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Не удалось сохранить ключ",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RemoveSelectedKey()
    {
        if (_keySelector.SelectedItem is not string selectedName)
        {
            MessageBox.Show(this, "Сначала выберите сохранённый ключ.", "GLM Switch",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show(this, $"Удалить ключ «{selectedName}»?", "GLM Switch",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        RunAction(() =>
        {
            _switcher.RemoveKey(selectedName);
            _keyNameBox.Clear();
            ReloadKeyNames();
        }, "Сохранённый ключ удалён.");
    }

    private void ReloadKeyNames(string? selectedName = null)
    {
        var names = _switcher.GetKeyNames();
        _keySelector.BeginUpdate();
        _keySelector.Items.Clear();
        foreach (var name in names)
        {
            _keySelector.Items.Add(name);
        }

        var selectedIndex = selectedName is null
            ? -1
            : names.FindIndex(name => string.Equals(name, selectedName, StringComparison.OrdinalIgnoreCase));
        if (selectedIndex < 0 && names.Count > 0)
        {
            selectedIndex = 0;
        }
        _keySelector.SelectedIndex = selectedIndex;
        _keySelector.EndUpdate();
    }

    private void RunAction(Action action, string successMessage)
    {
        try
        {
            action();
            _keyBox.Clear();
            ReloadKeyNames(_keySelector.SelectedItem as string);
            RefreshStatus();
            MessageBox.Show(this, successMessage, "GLM Switch", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Не удалось переключить провайдера", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshStatus()
    {
        try
        {
            var status = _switcher.GetStatus();
            _status.Text = $"Режим: {status.Mode}. Сохранённых ключей: {status.SavedKeyCount}.";
        }
        catch (Exception ex)
        {
            _status.Text = $"Не удалось прочитать настройки Claude Code: {ex.Message}";
            _status.ForeColor = System.Drawing.Color.Firebrick;
        }
    }
}

internal sealed class SettingsSwitcher
{
    private const string BaseUrl = "https://api.z.ai/api/anthropic";
    private const string StateFileName = "active-state.dat";
    private const string KeyStoreFileName = "glm-keys.dat";
    private const string LegacyKeyFileName = "glm-key.dat";
    private static readonly string[] ManagedKeys =
    [
        "ANTHROPIC_AUTH_TOKEN",
        "ANTHROPIC_BASE_URL",
        "ANTHROPIC_DEFAULT_HAIKU_MODEL",
        "ANTHROPIC_DEFAULT_SONNET_MODEL",
        "ANTHROPIC_DEFAULT_OPUS_MODEL",
        "CLAUDE_CODE_AUTO_COMPACT_WINDOW",
        "CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC",
        "API_TIMEOUT_MS"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _settingsPath;
    private readonly string _appDataDirectory;
    private readonly string _statePath;
    private readonly string _keyStorePath;
    private readonly string _legacyKeyPath;

    public SettingsSwitcher(string? settingsPath = null, string? appDataDirectory = null)
    {
        _settingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "settings.json");
        _appDataDirectory = appDataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GLM Switch");
        _statePath = Path.Combine(_appDataDirectory, StateFileName);
        _keyStorePath = Path.Combine(_appDataDirectory, KeyStoreFileName);
        _legacyKeyPath = Path.Combine(_appDataDirectory, LegacyKeyFileName);
    }

    public (string Mode, int SavedKeyCount) GetStatus()
    {
        var settings = ReadSettings();
        var baseUrl = settings["env"] is JsonObject env && env["ANTHROPIC_BASE_URL"] is JsonValue value
            ? value.GetValue<string>()
            : null;
        return (string.Equals(baseUrl, BaseUrl, StringComparison.OrdinalIgnoreCase) ? "GLM" : "Claude",
            LoadKeyEntries().Count);
    }

    public List<string> GetKeyNames() =>
        LoadKeyEntries().Select(entry => entry.Name).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();

    public void SaveKey(string? name, string? key)
    {
        var normalizedName = name?.Trim();
        var normalizedKey = key?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new InvalidOperationException("Укажите имя для ключа.");
        }

        if (normalizedName.Length > 64)
        {
            throw new InvalidOperationException("Имя ключа должно быть не длиннее 64 символов.");
        }

        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            throw new InvalidOperationException("Введите API-ключ.");
        }

        var entries = LoadKeyEntries();
        var existing = entries.FindIndex(entry =>
            string.Equals(entry.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
        var entry = new SavedKey(normalizedName, normalizedKey);
        if (existing < 0)
        {
            entries.Add(entry);
        }
        else
        {
            entries[existing] = entry;
        }
        SaveKeyEntries(entries);
    }

    public void RemoveKey(string name)
    {
        var entries = LoadKeyEntries();
        entries.RemoveAll(entry => string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase));
        if (entries.Count == 0)
        {
            if (File.Exists(_keyStorePath))
            {
                File.Delete(_keyStorePath);
            }
        }
        else
        {
            SaveKeyEntries(entries);
        }
    }

    public string SwitchToGlm(string? selectedName, string? newKeyName, string? newKey)
    {
        if (!string.IsNullOrWhiteSpace(newKey))
        {
            SaveKey(newKeyName, newKey);
            selectedName = newKeyName?.Trim();
        }

        if (string.IsNullOrWhiteSpace(selectedName))
        {
            throw new InvalidOperationException("Выберите сохранённый ключ или введите новый и укажите для него имя.");
        }

        var apiKey = FindKey(selectedName);
        var settings = ReadSettings();
        if (!File.Exists(_statePath))
        {
            var env = GetEnvObject(settings);
            if (env?["ANTHROPIC_BASE_URL"]?.GetValue<string>() is string currentUrl
                && string.Equals(currentUrl, BaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "GLM уже настроен, но сохранённое состояние переключателя не найдено. Настройки не изменены.");
            }

            var state = CaptureState(settings);
            CreateBackup();
            SaveEncrypted(_statePath, JsonSerializer.Serialize(state));
        }

        var targetEnv = EnsureEnvObject(settings);
        targetEnv["ANTHROPIC_AUTH_TOKEN"] = apiKey;
        targetEnv["ANTHROPIC_BASE_URL"] = BaseUrl;
        targetEnv["ANTHROPIC_DEFAULT_HAIKU_MODEL"] = "glm-5.3-flash[1m]";
        targetEnv["ANTHROPIC_DEFAULT_SONNET_MODEL"] = "glm-5.3[1m]";
        targetEnv["ANTHROPIC_DEFAULT_OPUS_MODEL"] = "glm-5.3[1m]";
        targetEnv["CLAUDE_CODE_AUTO_COMPACT_WINDOW"] = "1000000";
        targetEnv["CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC"] = "1";
        targetEnv["API_TIMEOUT_MS"] = "3000000";
        WriteSettings(settings);
        return selectedName;
    }

    public void SwitchToClaude()
    {
        if (!File.Exists(_statePath))
        {
            throw new InvalidOperationException(
                "Нет состояния GLM-переключения. Настройки не изменены — переключатель не будет удалять неизвестные переменные.");
        }

        var stateJson = LoadEncrypted(_statePath);
        var state = JsonSerializer.Deserialize<SwitchState>(stateJson)
            ?? throw new InvalidOperationException("Не удалось прочитать состояние переключателя.");
        var settings = ReadSettings();

        if (state.OriginalEnvWasNull)
        {
            settings["env"] = null;
        }
        else if (settings.TryGetPropertyValue("env", out var currentEnvValue))
        {
            if (currentEnvValue is not JsonObject env)
            {
                throw new InvalidOperationException(
                    "Поле env изменилось после включения GLM; настройки не изменены.");
            }

            foreach (var name in ManagedKeys)
            {
                if (state.OriginalValues.TryGetValue(name, out var originalJson))
                {
                    env[name] = JsonNode.Parse(originalJson);
                }
                else
                {
                    env.Remove(name);
                }
            }

            if (!state.HadEnvProperty && env.Count == 0)
            {
                settings.Remove("env");
            }
        }
        else if (state.HadEnvProperty)
        {
            throw new InvalidOperationException(
                "Поле env исчезло после включения GLM; настройки не изменены.");
        }

        WriteSettings(settings);
        File.Delete(_statePath);
    }

    private JsonObject ReadSettings()
    {
        if (!File.Exists(_settingsPath))
        {
            throw new FileNotFoundException("Не найден файл настроек Claude Code.", _settingsPath);
        }

        try
        {
            return JsonNode.Parse(File.ReadAllText(_settingsPath)) as JsonObject
                ?? throw new InvalidOperationException("Корень settings.json должен быть JSON-объектом.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("settings.json содержит некорректный JSON; файл не изменён.", ex);
        }
    }

    private static JsonObject? GetEnvObject(JsonObject settings)
    {
        if (!settings.TryGetPropertyValue("env", out var value) || value is null)
        {
            return null;
        }

        return value as JsonObject
            ?? throw new InvalidOperationException("Поле env в settings.json должно быть объектом; файл не изменён.");
    }

    private static JsonObject EnsureEnvObject(JsonObject settings)
    {
        var env = GetEnvObject(settings);
        if (env is not null)
        {
            return env;
        }

        env = new JsonObject();
        settings["env"] = env;
        return env;
    }

    private static SwitchState CaptureState(JsonObject settings)
    {
        var hadEnvProperty = settings.TryGetPropertyValue("env", out var originalEnv);
        if (originalEnv is not null && originalEnv is not JsonObject)
        {
            throw new InvalidOperationException("Поле env в settings.json должно быть объектом; файл не изменён.");
        }

        var originalValues = new Dictionary<string, string>();
        if (originalEnv is JsonObject env)
        {
            foreach (var name in ManagedKeys)
            {
                if (env.TryGetPropertyValue(name, out var value))
                {
                    originalValues[name] = value?.ToJsonString() ?? "null";
                }
            }
        }

        return new SwitchState(hadEnvProperty, hadEnvProperty && originalEnv is null, originalValues);
    }

    private void CreateBackup()
    {
        var suffix = DateTime.Now.ToString("yyyyMMdd-HHmmssfff");
        var backupPath = _settingsPath + ".before-glm-switch-" + suffix + ".bak";
        File.Copy(_settingsPath, backupPath, overwrite: false);
    }

    private List<SavedKey> LoadKeyEntries()
    {
        if (File.Exists(_keyStorePath))
        {
            return JsonSerializer.Deserialize<List<SavedKey>>(LoadEncrypted(_keyStorePath)) ?? [];
        }

        if (File.Exists(_legacyKeyPath))
        {
            var legacyKey = LoadEncrypted(_legacyKeyPath);
            var migrated = new List<SavedKey> { new("Ключ 1", legacyKey) };
            SaveKeyEntries(migrated);
            File.Delete(_legacyKeyPath);
            return migrated;
        }

        return [];
    }

    private void SaveKeyEntries(List<SavedKey> entries) =>
        SaveEncrypted(_keyStorePath, JsonSerializer.Serialize(entries));

    private string FindKey(string name)
    {
        var entry = LoadKeyEntries().FirstOrDefault(candidate =>
            string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
        return entry?.Key ?? throw new InvalidOperationException("Выбранный ключ не найден. Обновите список.");
    }

    private void WriteSettings(JsonObject settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath)
            ?? throw new InvalidOperationException("Не удалось определить папку settings.json.");
        var temporaryPath = Path.Combine(directory, $"settings.json.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporaryPath, settings.ToJsonString(JsonOptions), new UTF8Encoding(false));
            File.Move(temporaryPath, _settingsPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private void SaveEncrypted(string path, string value)
    {
        Directory.CreateDirectory(_appDataDirectory);
        var plaintext = Encoding.UTF8.GetBytes(value);
        try
        {
            var encrypted = ProtectedData.Protect(plaintext, optionalEntropy: null, DataProtectionScope.CurrentUser);
            var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporaryPath, encrypted);
                File.Move(temporaryPath, path, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private string LoadEncrypted(string path)
    {
        if (!File.Exists(path))
        {
            return string.Empty;
        }

        var encrypted = File.ReadAllBytes(path);
        var plaintext = ProtectedData.Unprotect(encrypted, optionalEntropy: null, DataProtectionScope.CurrentUser);
        try
        {
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private sealed record SwitchState(
        bool HadEnvProperty,
        bool OriginalEnvWasNull,
        Dictionary<string, string> OriginalValues);
    private sealed record SavedKey(string Name, string Key);

    public static void RunSelfTest()
    {
        if (!typeof(MainForm).Assembly.GetManifestResourceNames()
                .Contains("GLMSwitchApp.glm-switch.ico", StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Self-test failed: the window icon resource is missing.");
        }

        var root = Path.Combine(Path.GetTempPath(), "glm-switch-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var settingsPath = Path.Combine(root, "settings.json");
            var appDataPath = Path.Combine(root, "appdata");
            const string original = """
                {
                  "hooks": { "SessionStart": [] },
                  "env": {
                    "ANTHROPIC_BASE_URL": "https://original.example",
                    "KEEP_ME": "unchanged"
                  },
                  "enabledPlugins": ["keep"]
                }
                """;
            File.WriteAllText(settingsPath, original);
            var switcher = new SettingsSwitcher(settingsPath, appDataPath);
            switcher.SaveKey("Рабочий", "test-key-not-a-real-secret");
            switcher.SaveKey("Личный", "second-test-key");
            switcher.SwitchToGlm("Личный", null, null);

            var enabled = JsonNode.Parse(File.ReadAllText(settingsPath))!.AsObject();
            if (enabled["env"]?["ANTHROPIC_BASE_URL"]?.GetValue<string>() != BaseUrl
                || enabled["env"]?["ANTHROPIC_AUTH_TOKEN"]?.GetValue<string>() != "second-test-key"
                || enabled["env"]?["KEEP_ME"]?.GetValue<string>() != "unchanged"
                || enabled["hooks"]?["SessionStart"] is not JsonArray
                || enabled["enabledPlugins"]?[0]?.GetValue<string>() != "keep")
            {
                throw new InvalidOperationException("Self-test failed: GLM activation did not preserve unrelated settings.");
            }

            switcher.SwitchToClaude();
            var restored = JsonNode.Parse(File.ReadAllText(settingsPath))!.AsObject();
            if (restored["env"]?["ANTHROPIC_BASE_URL"]?.GetValue<string>() != "https://original.example"
                || restored["env"]?["ANTHROPIC_AUTH_TOKEN"] is not null
                || restored["env"]?["KEEP_ME"]?.GetValue<string>() != "unchanged"
                || restored["hooks"]?["SessionStart"] is not JsonArray
                || restored["enabledPlugins"]?[0]?.GetValue<string>() != "keep"
                || switcher.GetStatus().SavedKeyCount != 2)
            {
                throw new InvalidOperationException("Self-test failed: Claude restoration or key persistence failed.");
            }

            var encryptedKeyStore = File.ReadAllBytes(Path.Combine(appDataPath, KeyStoreFileName));
            var encryptedText = Encoding.UTF8.GetString(encryptedKeyStore);
            if (encryptedText.Contains("test-key-not-a-real-secret", StringComparison.Ordinal)
                || encryptedText.Contains("second-test-key", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Self-test failed: saved key profiles are not encrypted.");
            }

            var noEnvSettingsPath = Path.Combine(root, "settings-without-env.json");
            var noEnvAppDataPath = Path.Combine(root, "no-env-appdata");
            File.WriteAllText(noEnvSettingsPath, """{"hooks":{"PreToolUse":[]},"permissions":{"allow":[]}}""");
            var noEnvSwitcher = new SettingsSwitcher(noEnvSettingsPath, noEnvAppDataPath);
            noEnvSwitcher.SwitchToGlm(null, "Тестовый", "another-test-key");
            noEnvSwitcher.SwitchToClaude();
            var noEnvRestored = JsonNode.Parse(File.ReadAllText(noEnvSettingsPath))!.AsObject();
            if (noEnvRestored.ContainsKey("env")
                || noEnvRestored["hooks"]?["PreToolUse"] is not JsonArray
                || noEnvRestored["permissions"]?["allow"] is not JsonArray)
            {
                throw new InvalidOperationException("Self-test failed: a previously absent env block was not restored.");
            }

            Console.WriteLine("Self-test passed.");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
