#include "Clinic.h"
#include <cstring>
#include <vector>

using namespace Lab03;

// Структура для маршалинга в C#
#pragma pack(push, 1)
struct CPatient {
    int id;
    int type;
    char fullName[100];
    char address[200];
    int cardNumber;
    char diagnosis[100];
    int severity;
};
#pragma pack(pop)

// Хелпер копирования строки
void CopyStr(char* dest, size_t size, const std::string& src) {
    strncpy_s(dest, size, src.c_str(), size - 1);
}

// Конвертер из C++ класса в C-структуру
void ConvertToC(const Patient& p, CPatient& cp) {
    cp.id = p.GetId();
    cp.type = (int)p.GetType();
    CopyStr(cp.fullName, sizeof(cp.fullName), p.GetFullName());
    CopyStr(cp.address, sizeof(cp.address), p.GetAddress());
    cp.cardNumber = p.GetCardNumber();
    CopyStr(cp.diagnosis, sizeof(cp.diagnosis), p.GetDiagnosis());
    cp.severity = (int)p.GetSeverity();
}

// ==========================================
// ЭКСПОРТЫ ДЛЯ C#
// ==========================================
extern "C" {
    __declspec(dllexport) void* Lab03_Create() {
        return new Clinic();
    }

    __declspec(dllexport) void Lab03_Destroy(void* ptr) {
        if (ptr) delete static_cast<Clinic*>(ptr);
    }

    __declspec(dllexport) void Lab03_Clear(void* ptr) {
        if (ptr) static_cast<Clinic*>(ptr)->Clear();
    }

    __declspec(dllexport) int Lab03_AddPatient(void* ptr, int type, const char* name,
        const char* address, int cardNumber, const char* diagnosis, int severity)
    {
        if (!ptr) return -1;
        return static_cast<Clinic*>(ptr)->AddPatient(
            (PatientType)type, name, address, cardNumber, diagnosis, (Severity)severity
        );
    }

    __declspec(dllexport) bool Lab03_RemovePatient(void* ptr, int id) {
        if (!ptr) return false;
        return static_cast<Clinic*>(ptr)->RemovePatient(id);
    }

    // Универсальный метод для получения списка (сортированного или отфильтрованного)
    __declspec(dllexport) int Lab03_GetPatients(void* ptr, int mode, const char* filterStr,
        int minCard, int maxCard, CPatient* buffer, int maxSize)
    {
        if (!ptr) return 0;
        Clinic* clinic = static_cast<Clinic*>(ptr);
        std::vector<Patient> result;

        if (mode == 0) {
            // Все (с сортировкой)
            result = clinic->GetAllSorted();
        }
        else if (mode == 1 && filterStr != nullptr) {
            // Поиск по диагнозу
            result = clinic->FindByDiagnosis(filterStr);
        }
        else if (mode == 2) {
            // Поиск по диапазону карт
            result = clinic->FindByCardRange(minCard, maxCard);
        }

        int count = (int)result.size();
        if (count > maxSize) count = maxSize;

        for (int i = 0; i < count; i++) {
            ConvertToC(result[i], buffer[i]);
        }
        return count;
    }
}