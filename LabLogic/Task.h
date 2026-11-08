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
        int priority;             // 1..10, чем больше Ч тем важнее
        std::string dueDate;      // "yyyy-MM-dd" или пусто
        TaskStatus status;
    };
}
