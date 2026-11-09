#pragma once
#include <string>

namespace Lab02 {

    enum class TaskStatus : int {
        Pending = 0,
        InProgress = 1,
        Done = 2
    };

    struct Task {
        int id;
        std::string title;
        std::string description;
        int priority;
        std::string dueDate;
        std::string dueTime;   // "HH:mm", пусто = весь день
        TaskStatus status;
        bool isManual;
    };
}
