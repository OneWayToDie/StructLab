#pragma once
#include "Patient.h"
#include <vector>
#include <string>

namespace Lab03 {

    class Clinic {
    private:
        std::vector<Patient> patients;
        int nextId;
        void SortBySeverity(); // Приватный вспомогательный метод

    public:
        Clinic();

        void Clear();
        int AddPatient(PatientType type, const std::string& name, const std::string& addr,
            int card, const std::string& diag, Severity sev);
        bool RemovePatient(int id);

        std::vector<Patient> FindByDiagnosis(const std::string& diag) const;
        std::vector<Patient> FindByCardRange(int minCard, int maxCard) const;
        std::vector<Patient> GetAllSorted();
        std::vector<Patient> GetAll() const {
            return patients;
        }
    };
}