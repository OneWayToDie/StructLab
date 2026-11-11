#pragma once
#include <string>

namespace Lab03 {

    enum class PatientType : int { Human = 0, Animal = 1 };
    enum class Severity : int { Light = 0, Medium = 1, Critical = 2 };

    enum class Gender : int {
        Male = 0,
        Female = 1
    };

    class Patient {
    private:
        int id;
        PatientType type;
        Gender gender;
        std::string fullName;
        std::string address;
        int cardNumber;
        std::string diagnosis;
        Severity severity;

    public:
        Patient();
        Patient(int id, PatientType type, const std::string& name, const std::string& addr,
            int card, const std::string& diag, Severity sev);

        // Геттеры (короткие - можно оставить в .h)
        int GetId() const { return id; }
        PatientType GetType() const { return type; }
        std::string GetFullName() const { return fullName; }
        std::string GetAddress() const { return address; }
        int GetCardNumber() const { return cardNumber; }
        std::string GetDiagnosis() const { return diagnosis; }
        Severity GetSeverity() const { return severity; }
    };
}