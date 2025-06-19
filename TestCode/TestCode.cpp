// TestCode.cpp : 이 파일에는 'main' 함수가 포함됩니다. 거기서 프로그램 실행이 시작되고 종료됩니다.
//

#include <iostream>
#include "DataFrame.h"

#pragma pack(push,1)
struct Ty {
    char a;
    short b;
    char c;
    char d;
};
#pragma pack(pop)

using namespace RoSatProcessor;
int main()
{
    /*
    char* data = new char[16];
    DataFrameBuffer buf(data, 16);

    buf.sputc('a');
    buf.sputc('b');


    static constexpr char hexDigits[] = "0123456789ABCDEF";
    std::string result(16 * 2, '\0');

    for (size_t i = 0; i < (size_t)16; ++i) {
        unsigned char byte = static_cast<unsigned char>(data[i]);
        result[2 * i] = hexDigits[byte >> 4];
        result[2 * i + 1] = hexDigits[byte & 0xF];
    }
    std::cout << result << std::endl;
    */

    Ty type;

    std::string str = "abcd";
    DataFrame df;// (str.c_str(), str.size());
    df <<  0x46_b << 0x47_b << 0x48_b;
    df.normalize();

    char a, b, c, d;
    
    df >> a >> DataFrame::skip(1) >> b >> c >> d >> DataFrame::endl;
    std::cout << a << "/" << b << "/" << c << "/" << d << std::endl;


    
    df << 0x49_b;
    df >> type;
    std::cout << type.a << "/" << type.b << "/" << type.c << "/" << type.d << std::endl;

    std::cout << df << "\n";
    

    //std::cout << df.remain() << "/" << df.size() << std::endl;
    //df.put('c');
    //std::cout << df.remain() << "/" << df.size() << std::endl;
    //df.put('d');
    //std::cout << df.remain() << "/" << df.size() << std::endl;


    //
}

// 프로그램 실행: <Ctrl+F5> 또는 [디버그] > [디버깅하지 않고 시작] 메뉴
// 프로그램 디버그: <F5> 키 또는 [디버그] > [디버깅 시작] 메뉴

// 시작을 위한 팁: 
//   1. [솔루션 탐색기] 창을 사용하여 파일을 추가/관리합니다.
//   2. [팀 탐색기] 창을 사용하여 소스 제어에 연결합니다.
//   3. [출력] 창을 사용하여 빌드 출력 및 기타 메시지를 확인합니다.
//   4. [오류 목록] 창을 사용하여 오류를 봅니다.
//   5. [프로젝트] > [새 항목 추가]로 이동하여 새 코드 파일을 만들거나, [프로젝트] > [기존 항목 추가]로 이동하여 기존 코드 파일을 프로젝트에 추가합니다.
//   6. 나중에 이 프로젝트를 다시 열려면 [파일] > [열기] > [프로젝트]로 이동하고 .sln 파일을 선택합니다.
