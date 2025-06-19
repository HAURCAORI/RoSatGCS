# CodingStyle
## 목차

## 일반
### 명명법
- 파일 및 폴더 이름의 첫 글자는 무조건 대문자로 작성
- 윈도우 창의 경우 ```Window-```접두사 사용
- 페이지의 경우 ```Page-```접두사 사용
- ```ViewModels```의 경우 ```[View이름]ViewModel```로 지정
### 폴더 구성
- 필요한 기능은 ```Utils```폴더 내에 구현
- ```MVVM```디자인 패턴을 적용해 디자인은 ```Views```폴더에, 구현은 ```ViewModels```폴도에 구현

  
## C# 일반
### 명명법
- Method의 경우 첫 글자는 무조건 대문자로 작성한다.
- Field의 경우 ```mExample``` 형태로 선언한다.
### Doxygen
TBD
## XAML 일반
TBD

## Localization
- UI 디자인 시 이름을 지정할 때 ```Resources/Resource.resx```파일에 사용하는 텍스트 지정
- ```이름```항목은 사용하는 그룹별로 나누어 이름을 지정하거나, 범용적으로 사용되는 이름일 경우 접두사 ```s-```를 붙임
(ex. ```MenuFile```, ```MenuTool```, ```sApply```)
- ```중립 값```에는 영어 문자열, ```ko-KR```에는 한국어 문자열 작성