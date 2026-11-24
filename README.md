# GLM Switch

[Русский](#русский) | [English](#english)

## Русский

Небольшое приложение для Windows, которое переключает официальное расширение Claude Code for VS Code между Claude и GLM через настройки Claude Code.

### Возможности

- Переключение между Claude и совместимым с Anthropic API Z.AI.
- Именованные профили API-ключей: сохранение, выбор и удаление.
- Сохранённые профили шифруются Windows DPAPI и доступны только текущей учётной записи Windows.
- При возврате на Claude восстанавливаются прежние значения управляемых переменных; остальные настройки Claude Code сохраняются.
- Перед первым переключением создаётся копия `settings.json`.

### Использование

1. Запустите `GLM Switch.exe`.
2. Для нового ключа укажите имя и ключ, затем нажмите **Сохранить ключ**. Новый ключ также можно ввести и сразу включить GLM.
3. Для сохранённого ключа выберите профиль и нажмите **Переключить на GLM**. Чтобы убрать профиль, выберите его и нажмите **Удалить выбранный**.
4. Нажмите **Вернуть Claude**, чтобы восстановить настройки Claude.
5. После переключения перезапустите текущий чат Claude Code.

Пока GLM включён, активный ключ находится в `%USERPROFILE%\.claude\settings.json` в открытом виде: расширению Claude Code нужно прочитать его при запуске. Нажатие **Вернуть Claude** удаляет этот переопределённый ключ, но оставляет зашифрованный профиль. Не публикуйте `settings.json` и его резервные копии.

Настройки GLM: `https://api.z.ai/api/anthropic`, модели `glm-5.3[1m]` и `glm-5.3-flash[1m]`. Идентификаторы моделей взяты из рабочего примера и могут зависеть от доступности в плане Z.AI.

### Где получить ключ

Откройте [список API-ключей Z.AI](https://z.ai/manage-apikey/apikey-list), перейдите на вкладку **Personal Coding Plan** и создайте ключ. Сохраните его в приложении под отдельным именем.

Не используйте ключи со вкладки **Standard**: они предназначены для Standard Model API. Для приложения нужен ключ **Personal Coding Plan**.

### Требования

- Windows x64.
- .NET 8 Desktop Runtime.
- Claude Code for VS Code и `%USERPROFILE%\.claude\settings.json`.
- API-ключ Z.AI с доступом к API, совместимому с Anthropic.

### Сборка и проверка

Для локальной сборки требуются .NET 8 SDK и Windows Desktop Runtime:

```powershell
dotnet publish .\GLMSwitchApp.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o .\publish
.\publish\GLM Switch.exe --self-test
```

Публикация релиза автоматизирована через GitHub Actions: отправьте тег `v*` (например, `v0.1.0`). Workflow сверит тег с версией проекта, соберёт приложение на Windows, выполнит самопроверку и приложит EXE к GitHub Release.

## English

A small Windows utility that switches the official Claude Code for VS Code extension between Claude and GLM through Claude Code settings.

### Features

- Switch between Claude and Z.AI's Anthropic API-compatible service.
- Save, select, and remove named API key profiles.
- Profiles are encrypted with Windows DPAPI and can only be accessed by the current Windows account.
- Switching back to Claude restores the previous values of managed variables while preserving other Claude Code settings.
- A backup of `settings.json` is created before the first switch.

### Usage

1. Run `GLM Switch.exe`.
2. To add a key, enter a name and the key, then select **Save key**. You can also enter a new key and switch to GLM immediately.
3. To use a saved key, select its profile and choose **Switch to GLM**. To remove a profile, select it and choose **Remove selected**.
4. Choose **Restore Claude** to restore Claude settings.
5. Restart the current Claude Code chat after switching.

While GLM is enabled, the active key is stored in plain text in `%USERPROFILE%\.claude\settings.json` so the Claude Code extension can read it at startup. **Restore Claude** removes that override but keeps the encrypted profile. Do not publish `settings.json` or its backups.

GLM settings: `https://api.z.ai/api/anthropic`, using models `glm-5.3[1m]` and `glm-5.3-flash[1m]`. These model identifiers come from the working example and may depend on availability in your Z.AI plan.

### Getting an API key

Open the [Z.AI API keys page](https://z.ai/manage-apikey/apikey-list), select the **Personal Coding Plan** tab, and create a key. Save it in the app under a distinct profile name.

Do not use keys from the **Standard** tab; those are for the Standard Model API. This app requires a **Personal Coding Plan** key.

### Requirements

- Windows x64.
- .NET 8 Desktop Runtime.
- Claude Code for VS Code and `%USERPROFILE%\.claude\settings.json`.
- A Z.AI API key with access to the Anthropic API-compatible service.

### Build and test

The local build requires the .NET 8 SDK and Windows Desktop Runtime:

```powershell
dotnet publish .\GLMSwitchApp.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o .\publish
.\publish\GLM Switch.exe --self-test
```

GitHub Actions builds and publishes releases. Push a `v*` tag (for example, `v0.1.0`); the workflow checks that the tag matches the project version, builds on Windows, runs the self-test, and attaches the EXE to a GitHub Release.
