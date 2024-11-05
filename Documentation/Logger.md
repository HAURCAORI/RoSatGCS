# Logger
```RoSatGCS``` 프로그램에서는 ```NLog```을 사용하여 로그 정보를 기록한다. 사용하는 ```NLog```의 버전은 5.3.4이고, NuGet 패키지 관리에서 확인할 수 있다.
## 로그 파일 지정
프로젝트 최상위 디렉토리의 ```NLog.config```파일을 이용해 로그 방식과 대상을 지정할 수 있다.

## 클래스 작성 요령
```c#
class Class{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
}
```