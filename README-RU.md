# AMWin-RP
![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/PKBeam/AMWin-RP/total) ![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/PKBeam/AMWin-RP/latest/total) &nbsp; ([English](README.md) | [Korean](README-KO.md) | [Japanese](README-JA.md) | [Español](README-ES.md))

Клиент Discord Rich Presence для нативного приложения Apple Music на Windows.  
Также включает скробблинг в Last.FM и ListenBrainz.

<image width=450 src="https://github.com/user-attachments/assets/df5d6a83-4630-4384-b521-bc80c286a499" />
&nbsp; &nbsp; 
<image src=https://github.com/user-attachments/assets/ea63ddf1-d822-4ffd-be9d-24e13701fce9 width=300 />

## Установка
AMWin-RP требует Windows 11 24H2 или новее.

Сборки доступны [здесь](https://github.com/PKBeam/AMWin-RP/releases).

### Какой релиз выбрать?
Выберите `x64` или `ARM64` в зависимости от процессора вашего ПК.  
Затем выберите один из двух файлов: стандартный или помеченный как `NoRuntime`.

Если не уверены, используйте релиз без метки (то есть без `NoRuntime`).  
Эта версия подходит для всех, но занимает больше места, так как включает компоненты .NET, необходимые для запуска приложения.

Релиз `NoRuntime` значительно меньше, но требует установленного [.NET 10 desktop runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).  
Если рантайм не установлен, приложение предложит установить его при запуске.

## Использование
Для работы AMWin-RP нужна [версия Apple Music из Microsoft Store](https://apps.microsoft.com/detail/9PFHDD62MXS1).

- Откройте `.exe`, чтобы запустить приложение.
- AMWin-RP работает в фоне и сворачивается в системный трей.
- Двойной клик по иконке в трее открывает окно настроек.
  - Здесь можно настроить отдельные параметры: автозапуск, скробблинг и определение треков.
- Закрыть приложение можно через правый клик по иконке в трее и пункт "Exit".
- По умолчанию, чтобы отображался Rich Presence, приложение Apple Music должно быть открыто и воспроизводить музыку (не на паузе).

**Примечание**: если вы используете виртуальные рабочие столы, AMWin-RP и Apple Music должны находиться на одном рабочем столе.  
Это техническое ограничение библиотеки UI Automation, которая используется для парсинга клиентского приложения Apple Music.

## Скробблинг
Текущая реализация скробблинга не поддерживает офлайн-скробблы. Это означает, что треки, прослушанные без подключения к интернету, будут потеряны.

### Last.FM
Вам понадобятся собственные API Key и API Secret от Last.FM.  
Чтобы получить их, перейдите на https://www.last.fm/api и выберите "Get an API Account."  
Введите эти данные в настройках вместе с именем пользователя и паролем Last.FM.

Пароль Last.FM хранится в [Windows Credential Manager](https://support.microsoft.com/en-us/windows/accessing-credential-manager-1b5c916a-6a16-889f-8581-fc16e8165ac0) в рамках вашей локальной учетной записи Windows.

### ListenBrainz
Вы можете скробблить в ListenBrainz, добавив пользовательский токен в настройках.

## Сообщение об ошибках
Перед созданием нового issue убедитесь, что ваша проблема не дублирует уже существующую.
Если вы сообщаете о проблеме, приложите соответствующие `.log`-файлы (они находятся в `%localappdata%\\AMWin-RichPresence`).

Перед публикацией проверьте следующее:
- Проблема еще не описана в существующем открытом или закрытом issue.
- В Discord включено отображение активности (Settings > Activity Settings > Activity Privacy > Activity Status).
