#include "Clinic.h"
#include <algorithm>

namespace Lab03 {
    Clinic::Clinic() : nextId(1) {}

    void Clinic::Clear() {
        patients.clear();
        nextId = 1;
    }

    int Clinic::AddPatient(PatientType type, const std::string& name, const std::string& addr,
        int card, const std::string& diag, Severity sev) {
        Patient p(nextId++, type, name, addr, card, diag, sev);
        patients.push_back(p);
        return p.GetId();
    }

    bool Clinic::RemovePatient(int id) {
        auto it = std::remove_if(patients.begin(), patients.end(),
            [id](const Patient& p) { return p.GetId() == id; });

        if (it != patients.end()) {
            patients.erase(it, patients.end());
            return true;
        }
        return false;
    }

    void Clinic::SortBySeverity() {
        std::sort(patients.begin(), patients.end(), [](const Patient& a, const Patient& b) {
            return static_cast<int>(a.GetSeverity()) > static_cast<int>(b.GetSeverity());
            });
    }

    std::vector<Patient> Clinic::FindByDiagnosis(const std::string& diag) const {
        std::vector<Patient> result;
        for (const auto& p : patients) {
            if (p.GetDiagnosis().find(diag) != std::string::npos) {
                result.push_back(p);
            }
        }
        return result;
    }

    std::vector<Patient> Clinic::FindByCardRange(int minCard, int maxCard) const {
        std::vector<Patient> result;
        for (const auto& p : patients) {
            if (p.GetCardNumber() >= minCard && p.GetCardNumber() <= maxCard) {
                result.push_back(p);
            }
        }
        return result;
    }

    std::vector<Patient> Clinic::GetAllSorted() {
        SortBySeverity();
        return patients;
    }
}