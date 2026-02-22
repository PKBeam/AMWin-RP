# AMWin-RP
![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/PKBeam/AMWin-RP/total) ![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/PKBeam/AMWin-RP/latest/total) &nbsp; ([English](README.md) | [日本語](README-JA.md))

Apple Music 네이티브 Windows 앱용 Discord Rich Presence 클라이언트입니다.  
Last.FM 및 ListenBrainz 스크로블링도 지원합니다.

<image width=450 src="https://github.com/user-attachments/assets/df5d6a83-4630-4384-b521-bc80c286a499" />
&nbsp; &nbsp; 
<image src=https://github.com/user-attachments/assets/ea63ddf1-d822-4ffd-be9d-24e13701fce9 width=300 />

## 설치
AMWin-RP는 Windows 11 24H2 이상이 필요합니다.

빌드는 [여기](https://github.com/PKBeam/AMWin-RP/releases)에서 확인할 수 있습니다.

### 어떤 릴리스를 사용해야 하나요?
PC 프로세서에 맞게 x64 또는 ARM64를 선택하세요.  
그다음 표준 버전과 `NoRuntime` 버전 중 하나를 선택하면 됩니다.

잘 모르겠다면 `NoRuntime`이 붙지 않은 기본 릴리스를 사용하세요.  
이 버전은 앱 실행에 필요한 .NET 구성 요소를 함께 포함하므로 파일 크기가 더 큽니다.

`NoRuntime` 릴리스는 더 작지만 [.NET 10 데스크톱 런타임](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)이 설치되어 있어야 합니다.  
런타임이 설치되어 있지 않으면 앱 실행 시 설치 안내가 표시됩니다.

## 사용 방법
AMWin-RP를 사용하려면 Apple Music의 [Microsoft Store 버전](https://apps.microsoft.com/detail/9PFHDD62MXS1)이 필요합니다.

- `.exe`를 실행하면 앱이 시작됩니다.
- AMWin-RP는 백그라운드에서 실행되며 시스템 트레이에 최소화됩니다.
- 트레이 아이콘을 더블 클릭하면 설정 창이 열립니다.
  - 여기서 시작 프로그램 실행, 스크로블링, 곡 감지 등의 옵션을 조정할 수 있습니다.
- 앱을 종료하려면 트레이 아이콘을 우클릭한 뒤 `Exit`를 선택하세요.
- 기본 설정에서는 Apple Music 앱이 열려 있고 음악이 재생 중(일시정지 아님)이어야 Rich Presence가 표시됩니다.

**참고**: 가상 데스크톱을 사용한다면 AMWin-RP와 Apple Music이 같은 데스크톱에 있어야 합니다.  
이것은 Apple Music 클라이언트 정보를 추출하는 UI Automation 라이브러리의 기술적 제한 때문입니다.

## 스크로블링
현재 스크로블러 구현은 오프라인 스크로블링을 지원하지 않으므로, 인터넷 연결이 없는 상태에서 들은 곡은 기록되지 않습니다.

### Last.FM
Last.FM API Key와 API Secret이 필요합니다.  
생성하려면 https://www.last.fm/api 로 이동한 뒤 `Get an API Account.`를 선택하세요.  
설정 메뉴에서 Last.FM 사용자 이름/비밀번호와 함께 입력하면 됩니다.

Last.FM 비밀번호는 로컬 Windows 계정의 [Windows 자격 증명 관리자](https://support.microsoft.com/en-us/windows/accessing-credential-manager-1b5c916a-6a16-889f-8581-fc16e8165ac0)에 저장됩니다.

### ListenBrainz
설정에 사용자 토큰을 입력하면 ListenBrainz로 스크로블링할 수 있습니다.

## 버그 제보
새 이슈를 만들기 전에, 같은 문제가 기존 이슈에 없는지 먼저 확인해 주세요.  
문제를 제보할 때는 관련 `.log` 파일(`%localappdata%\\AMWin-RichPresence`)을 함께 첨부해 주세요.

등록 전 아래 항목을 확인해 주세요:
- 문제가 이미 열린/닫힌 이슈에 포함되어 있지 않은지
- Discord에서 RP 표시가 활성화되어 있는지 (Settings > Activity Settings > Activity Privacy > Activity Status)
