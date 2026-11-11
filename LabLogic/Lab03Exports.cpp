#include "Clinic.h"
#include <cstring>
#include <vector>
#include <string>
#include <algorithm> // <--- ДОБАВЛЕНО (для std::sort)

using namespace Lab03;

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

namespace {
    void CopyStrLocal(char* dest, size_t size, const std::string& src) {
        strncpy_s(dest, size, src.c_str(), size - 1);
    }

    void ConvertToC(const Patient& p, CPatient& cp) {
        cp.id = p.GetId();
        cp.type = (int)p.GetType();
        CopyStrLocal(cp.fullName, sizeof(cp.fullName), p.GetFullName());
        CopyStrLocal(cp.address, sizeof(cp.address), p.GetAddress());
        cp.cardNumber = p.GetCardNumber();
        CopyStrLocal(cp.diagnosis, sizeof(cp.diagnosis), p.GetDiagnosis());
        cp.severity = (int)p.GetSeverity();
    }
}

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

    __declspec(dllexport) int Lab03_GetPatients(void* ptr, const char* filterStr,
        int minCard, int maxCard, CPatient* buffer, int maxSize)
    {
        if (!ptr) return 0;
        Clinic* clinic = static_cast<Clinic*>(ptr);

        // Теперь метод GetAll() существует и работает
        std::vector<Patient> all = clinic->GetAll();
        std::vector<Patient> result;

        for (const auto& p : all) {
            bool matchStr = (filterStr == nullptr || strlen(filterStr) == 0 ||
                p.GetDiagnosis().find(filterStr) != std::string::npos);
            bool matchCard = (p.GetCardNumber() >= minCard && p.GetCardNumber() <= maxCard);

            if (matchStr && matchCard) {
                result.push_back(p);
            }
        }

        // std::sort теперь работает благодаря <algorithm>
        std::sort(result.begin(), result.end(), [](const Patient& a, const Patient& b) {
            return static_cast<int>(a.GetSeverity()) > static_cast<int>(b.GetSeverity());
            });

        int count = (int)result.size();
        if (count > maxSize) count = maxSize;

        for (int i = 0; i < count; i++) {
            ConvertToC(result[i], buffer[i]);
        }
        return count;
    }
}