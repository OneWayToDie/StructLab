#include "Patient.h"

namespace Lab03 {
    Patient::Patient()
        : id(0), type(PatientType::Human), cardNumber(0), severity(Severity::Light) {
    }

    Patient::Patient(int id, PatientType type, const std::string& name, const std::string& addr,
        int card, const std::string& diag, Severity sev)
        : id(id), type(type), fullName(name), address(addr),
        cardNumber(card), diagnosis(diag), severity(sev) {
    }
}