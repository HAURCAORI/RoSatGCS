# RoSatGCSDevelopment
## 목차
1. [Logger](#logger)


## 프로젝트 구성
```bash
RoSatGCS
|-- Controls            : 사용자 컨트롤 관련 정의
|-- Fonts               : 폰트 정의
|-- Images              : 이미지
|-- Models              : 공용 클래스
|-- Resources           : 현지화 문자열 저장
|-- Styles              : 스타일 프리셋 정의
|-- Utils
|   |-- Converter       : 변환 함수 정의
|   |-- Drawing         : 도형 및 텍스트 그리는 함수
|   |-- Exception       : 예외 정의
|   |-- Localization    : 현지화 함수
|   |-- Maps            : Ground Track 관련 툴
|   |-- Navigation      : GUI Page 이동 관련 정의
|   |-- Satellites      : 위성 궤도 관련 툴
|
|-- ViewModels          : UI 구현
|-- Views               : UI 디자인
|-- App.xaml            : 프로그램 진입점 및 리소스 설정
|-- AssemblyInfo.cs     : 어셈블리 정보
|-- NLog.config         : 로그 옵션 설정 파일
|-- Types.cs            : 타입 정의
```


## Logger
```RoSatGCS``` 프로그램에서는 ```NLog```을 사용하여 로그 정보를 기록한다. 사용하는 ```NLog```의 버전은 5.3.4이고, NuGet 패키지 관리에서 확인할 수 있다.
### 로그 파일 설정
프로젝트 최상위 디렉토리의 ```NLog.config```파일을 이용해 로그 방식과 대상을 지정할 수 있다.
### target 속성
|속성|설명|
|---|---|
|name|이름 지정|
|xsi:type|출력 형식 지정|
|fileName|파일 이름 및 경로 지정|
|archiveEvery|아카이브 생성 주기|
|archiveDateFormat|아카이브 날짜 포멧|
|archiveNumbering|아카이브 번호 규칙|
|archiveFileName|아카이브 파일 이름|
|archiveAboveSize|최대 아카이브 파일 크기|
|maxArchiveFiles|최대 아카이브 파일 개수|
|maxArchiveDays|최대 아카이브 날짜
### 로그 Level
|항목|내용|
|---|---|
|Trace|Verbose로 사용|
|Debug|디버깅 시 사용|
|Info|정보 관련 내용|
|Warn|경고 관련 내용|
|Error|에러 관련 내용|
|Fatal|치명적 오류|

### 클래스 작성 요령
```c#
class Class{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
}
```
