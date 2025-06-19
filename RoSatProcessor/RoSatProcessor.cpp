#include "pch.h"
#include <iostream>
#include <string>
#include <tchar.h>
#include <conio.h>

#include "TaskPrint.h"
#include "TaskPacketProcess.h"
#include "TaskPacketReceiveQueue.h"
#include "TaskPacketTransmitQueue.h"
#include "TaskSerial.h"
#include "TaskQueryRequestHandler.h"
#include "TaskQueryResponseSender.h"
#include "TaskWebSocketConnector.h"
#include "RoSatTaskManager.h"

#include "ServiceImpl.h"
#include "ServiceInstaller.h"

#include "base64.h"



#define SERVICE_NAME            TEXT("RoSatProcessor")
#define SERVICE_DISPLAY_NAME    TEXT("RoSat Processor")
#define SERVICE_START_TYPE      SERVICE_DEMAND_START
#define SERVICE_DEPENDENCIES    TEXT("")
#define SERVICE_ACCOUNT         TEXT("NT AUTHORITY\\NetworkService")//LocalService
#define SERVICE_PASSWORD        NULL


void TaskDefinition() {
    //auto taskPacketReceiveQueue = RoSatProcessor::TaskPacketReceiveQueue(SERVICE_NAME, TEXT("PacketReceiveQueue"));
    //RoSatProcessor::RoSatTaskManager::addTask(taskPacketReceiveQueue);

    //auto taskPacketProcess = RoSatProcessor::TaskPacketProcess(SERVICE_NAME, TEXT("PacketProcess"));
    //RoSatProcessor::RoSatTaskManager::addTask(taskPacketProcess);

    //auto taskPacketTransmitQueue = RoSatProcessor::TaskPacketTransmitQueue(SERVICE_NAME, TEXT("PacketTransmitQueue"));
    //RoSatProcessor::RoSatTaskManager::addTask(taskPacketTransmitQueue);

    //auto taskPrint = RoSatProcessor::TaskPrint(SERVICE_NAME, TEXT("Print"));
    //RoSatProcessor::RoSatTaskManager::addTask(taskPrint);

    //auto taskSerial = RoSatProcessor::TaskSerial(SERVICE_NAME, TEXT("Serial"));
    //RoSatProcessor::RoSatTaskManager::addTask(taskSerial);

    auto taskQueryRequest = RoSatProcessor::TaskQueryRequestHandler(SERVICE_NAME, TEXT("QueryRequest"));
    RoSatProcessor::RoSatTaskManager::addTask(taskQueryRequest);

	auto taskQueryResponse = RoSatProcessor::TaskQueryResponseSender(SERVICE_NAME, TEXT("QueryResponse"));
	RoSatProcessor::RoSatTaskManager::addTask(taskQueryResponse);

	auto taskWebSocketConnector = RoSatProcessor::TaskWebSocketConnector(SERVICE_NAME, TEXT("WebSocketConnector"));
	RoSatProcessor::RoSatTaskManager::addTask(taskWebSocketConnector);


}

int wmain(int argc, TCHAR* argv[])
{
    RoSatProcessor::Config::Import();
    if ((argc > 1) && ((*argv[1] == L'-') || (*argv[1] == L'/')))
    {
        if (_tcsicmp(argv[1] + 1, TEXT("install")) == 0)
        {
            // Install the service when the command is 
            // "-install" or "/install".
            InstallService(
                SERVICE_NAME,               // Name of service
                SERVICE_DISPLAY_NAME,       // Name to display
                SERVICE_START_TYPE,         // Service start type
                SERVICE_DEPENDENCIES,       // Dependencies
                SERVICE_ACCOUNT,            // Service running account
                SERVICE_PASSWORD            // Password of the account
            );
        }
        else if (_tcsicmp(argv[1] + 1, TEXT("remove")) == 0)
        {
            // Uninstall the service when the command is 
            // "-remove" or "/remove".
            UninstallService(SERVICE_NAME);
         }
        else if (_tcsicmp(argv[1] + 1, TEXT("reinstall")) == 0)
        {
            UninstallService(SERVICE_NAME);
            InstallService(
                SERVICE_NAME,               // Name of service
                SERVICE_DISPLAY_NAME,       // Name to display
                SERVICE_START_TYPE,         // Service start type
                SERVICE_DEPENDENCIES,       // Dependencies
                SERVICE_ACCOUNT,            // Service running account
                SERVICE_PASSWORD            // Password of the account
            );
        }
        else if (_tcsicmp(argv[1] + 1, TEXT("debug")) == 0)
        {
            TaskDefinition();
            RoSatProcessor::RoSatTaskManager::run(NULL);

            while (true) {
                if (_kbhit()) {
                    char ch = _getch();
                    if (ch == 'q')
                    {
                        break;
                    }
                    else if (ch == 'r')
                    {
                        RoSatProcessor::RoSatTaskManager::run(NULL);
                    }
                    else if (ch == 's')
                    {
                        RoSatProcessor::RoSatTaskManager::stop();
                    }
                    else if (ch == 'c') {
                        system("cls");
                    }
                }
                Sleep(500);
            }
        }
    }
    else
    {
        wprintf(L"Parameters:\n");
        wprintf(L" -install  to install the service.\n");
        wprintf(L" -remove   to remove the service.\n");
        wprintf(L" -debug    to debug the service.\n");

        TaskDefinition();

        CServiceImpl service(const_cast<PWSTR>(SERVICE_NAME));
        if(!CServiceBase::Run(service))
        {
            wprintf(L"Service failed to run w/err 0x%08lx\n", GetLastError());
        }
    }

    return 0;
}


/*
FIDL::FIDLParser* parser = FIDL::CreateParser();

std::string path = R"(C:\Users\sunny\Desktop\RoSat\FIDL)";
std::string savepath = R"(C:\Users\sunny\Desktop\RoSat\FIDLResult\)";

for (const auto& file : std::filesystem::directory_iterator(path)) {
    if (file.path().extension() != ".fidl") {
        continue;
    }
    auto start = std::chrono::high_resolution_clock::now();

    std::cout << "Parsing:" << file.path().filename() << std::endl;

    std::string resultfile = file.path().filename().string();
    resultfile = resultfile.substr(0, resultfile.find_first_of('.')) + ".json";

    auto ret = FIDL::ParseStringToFile(parser, file.path().string(), savepath + resultfile);
    if (ret != ParseResult::Success) {
        std::cout << "Parse Error" << std::endl;
        StringResult str = FIDL::GetResult(parser);
        std::cout << str << std::endl;
        DisposeString(str);
        break;
    }
    auto milliseconds = std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::high_resolution_clock::now() - start);
    std::cout << "->Elapsed:" << milliseconds.count() << "ms" << std::endl;

}
return 0;

path = R"(C:\Users\sunny\Desktop\RoSat\FIDL\ConOps.fidl)";
auto ret = FIDL::ParseString(parser, path);

StringResult str = FIDL::GetResult(parser);
std::cout << str << std::endl;
DisposeString(str);

*/