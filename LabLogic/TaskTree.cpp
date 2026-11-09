#include "TaskTree.h"
#include <algorithm>
#include <cstdlib>
#include <ctime>

namespace Lab02 {

    TaskTree::TaskTree()
        : bstIndex(new BST()), avlIndex(new AVLTree()),
        useAvl(false), nextSequentialId(1)
    {
        static bool seeded = false;
        if (!seeded) { std::srand((unsigned)std::time(nullptr)); seeded = true; }
    }

    TaskTree::~TaskTree() { delete bstIndex; delete avlIndex; }

    int TaskTree::GenerateRandomId() {
        for (int attempt = 0; attempt < 200; attempt++) {
            int id = (std::rand() % 9999) + 1;
            if (tasks.find(id) == tasks.end()) return id;
        }
        for (int id = 1; id < 100000; id++)
            if (tasks.find(id) == tasks.end()) return id;
        return -1;
    }

    int TaskTree::AddTask(const std::string& title, const std::string& desc,
        int priority, const std::string& dueDate, const std::string& dueTime,
        bool sequential, bool isManual)
    {
        priority = std::max(1, std::min(10, priority));
        int id;
        if (sequential) {
            while (tasks.find(nextSequentialId) != tasks.end()) nextSequentialId++;
            id = nextSequentialId++;
        }
        else {
            id = GenerateRandomId();
        }
        if (id < 0) return -1;

        Task t;
        t.id = id;
        t.title = title;
        t.description = desc;
        t.priority = priority;
        t.dueDate = dueDate;
        t.dueTime = dueTime;   // ← НОВОЕ
        t.status = TaskStatus::Pending;
        t.isManual = isManual;

        tasks[id] = t;
        bstIndex->Insert(id);
        avlIndex->Insert(id);
        return id;
    }

    bool TaskTree::AddTaskWithId(int id, const std::string& title, const std::string& desc,
        int priority, const std::string& dueDate, const std::string& dueTime,
        TaskStatus status, bool isManual)
    {
        if (id <= 0) return false;
        if (tasks.find(id) != tasks.end()) return false;

        priority = std::max(1, std::min(10, priority));

        Task t;
        t.id = id;
        t.title = title;
        t.description = desc;
        t.priority = priority;
        t.dueDate = dueDate;
        t.dueTime = dueTime;
        t.status = status;
        t.isManual = isManual;

        tasks[id] = t;
        bstIndex->Insert(id);
        avlIndex->Insert(id);
        if (id >= nextSequentialId) nextSequentialId = id + 1;
        return true;
    }

    bool TaskTree::DeleteTask(int taskId) {
        auto it = tasks.find(taskId);
        if (it == tasks.end()) return false;
        tasks.erase(it);
        bstIndex->Delete(taskId);
        avlIndex->Delete(taskId);
        return true;
    }

    bool TaskTree::SetStatus(int taskId, TaskStatus status) {
        auto it = tasks.find(taskId);
        if (it == tasks.end()) return false;
        it->second.status = status;
        return true;
    }

    bool TaskTree::GetTask(int taskId, Task& out) const {
        auto it = tasks.find(taskId);
        if (it == tasks.end()) return false;
        out = it->second;
        return true;
    }

    std::vector<Task> TaskTree::GetAllTasks() const {
        std::vector<Task> out;
        out.reserve(tasks.size());
        for (auto& kv : tasks) out.push_back(kv.second);
        std::sort(out.begin(), out.end(),
            [](const Task& a, const Task& b) { return a.id < b.id; });
        return out;
    }

    std::vector<int> TaskTree::GetTaskIdsWithPriorityRange(int lo, int hi) const {
        std::vector<int> result;
        for (auto& kv : tasks)
            if (kv.second.priority >= lo && kv.second.priority <= hi)
                result.push_back(kv.first);
        return result;
    }

    std::vector<int> TaskTree::GetTaskIdsWithPriorityAtLeast(int minPriority) const {
        std::vector<int> result;
        for (auto& kv : tasks)
            if (kv.second.priority >= minPriority)
                result.push_back(kv.first);
        return result;
    }

    std::vector<int> TaskTree::GetTopUrgentIncomplete(int n) const {
        std::vector<const Task*> urgent;
        urgent.reserve(tasks.size());
        for (auto& kv : tasks)
            if (kv.second.status != TaskStatus::Done)
                urgent.push_back(&kv.second);
        std::sort(urgent.begin(), urgent.end(),
            [](const Task* a, const Task* b) { return a->priority > b->priority; });
        std::vector<int> result;
        for (size_t i = 0; i < urgent.size() && (int)result.size() < n; i++)
            result.push_back(urgent[i]->id);
        return result;
    }

    void TaskTree::Clear() {
        tasks.clear();
        bstIndex->Clear();
        avlIndex->Clear();
        nextSequentialId = 1;
    }

    std::vector<TaskTree::SnapshotNode> TaskTree::GetSnapshot() const {
        auto raw = CurrentTree()->GetSnapshot();
        std::vector<SnapshotNode> out;
        out.reserve(raw.size());
        for (auto& s : raw) {
            SnapshotNode sn;
            sn.key = s.key;
            sn.parentIndex = s.parentIndex;
            sn.isLeft = s.isLeft;
            auto it = tasks.find(s.key);
            sn.priority = (it != tasks.end()) ? it->second.priority : 0;
            out.push_back(sn);
        }
        return out;
    }
}