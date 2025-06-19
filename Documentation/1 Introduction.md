# Introduction

```RoSatGCS```는 ```RoSat```위성의 지상 관제를 위해 개발된 프로그램으로 하위 세 개의 프로그램으로 구성된다.
1. ```RoSatGCS(GUI)```
2. ```RoSatParser```
3. ```RoSatProcessor```

```RoSatGCS(GUI)```는 위성의 정보를 시각화하고, 스케줄을 관리하며, 안테나 지향 및 통신을 담당하는 프로그램으로 C#언어로 개발되었다. 위성의 식별 및 제어와 관련된 부분은 해당 프로그램을 통해 통합적으로 관리된다.

```RoSatParser```는 패킷 데이터를 파싱하거나 위성에서 사용되는 명령을 읽어오는 기능을 수행하는 프로그램으로 ISO C++17으로 작성되었다. 해당 프로그램은 라이브러리 형태로 사용되며, 윈도우의 동적 라이브러리 ```DLL```로 빌드되어 ```RoSatGCS(GUI)``` 및 ```RoSatProcessor```에서 활용된다.

```RoSatProcessor```는 실시간 데이터 처리 및 통신을 수행하는 백그라운드 프로세스로 ISO C++17을 이용해 작성되었다. 해당 프로세스는 ```RoSatGCS(GUI)```와 독립적으로 동작하며, ```GNU Radio```에서 받은 위성 데이터를 처리 및 가공한다.

## Documentation 목록
|번호|이름|내용|
|---|---|---|
|1|Introduction|프로젝트 소개|
|2|Requirements|요구사항|
|3|CodingStyle|코딩 스타일 정의|
|4|RoSatGCSDevelopment|```RoSatGCS(GUI)```개발 자료|
|5|RoSatParserDevelopment|```RoSatParser```개발 자료|
|6|RoSatProcessorDevelopment|```RoSatProcessoer``` 개발 자료|   
# 테스트