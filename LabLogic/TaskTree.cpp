#include "TaskTree.h"
#include <algorithm>

namespace Lab02 {

    TaskTree::TaskTree()
        : bstIndex(new BST()), avlIndex(new AVLTree()), useAvl(false), nextId(1)
    {
    }

    TaskTree::~TaskTree() {
        delete bstIndex;
        delete avlIndex;
    }

    void TaskTree::SyncIndexes() {
        // ѕересобираем оба дерева из текущих приоритетов
        bstIndex->Clear();
        avlIndex->Clear();
        for (auto& kv : tasksByPriority) {
            int priority = kv.first;
            if (!kv.second.empty()) {
                bstIndex->Insert(priority);
                avlIndex->Insert(priority);
            }
        }
    }

    int TaskTree::AddTask(const std::string& title, const std::string& desc,
        int priority, const std::string& dueDate)
    {
        priority = std::max(1, std::min(10, priority));

        Task t;
        t.id = nextId++;
        t.title = title;
        t.description = desc;
        t.priority = priority;
        t.dueDate = dueDate;
        t.status = TaskStatus::Pending;

        tasks[t.id] = t;

        auto it = tasksByPriority.find(priority);
        if (it == tasksByPriority.end()) {
            tasksByPriority[priority] = std::vector<int>{ t.id };
            bstIndex->Insert(priority);
            avlIndex->Insert(priority);
        }
        else {
            it->second.push_back(t.id);
        }

        return t.id;
    }

    bool TaskTree::DeleteTask(int taskId) {
        auto it = tasks.find(taskId);
        if (it == tasks.end()) return false;

        int priority = it->second.priority;
        tasks.erase(it);

        auto pit = tasksByPriority.find(priority);
        if (pit != tasksByPriority.end()) {
            auto& ids = pit->second;
            ids.erase(std::remove(ids.begin(), ids.end(), taskId), ids.end());
            if (ids.empty()) {
                tasksByPriority.erase(pit);
                bstIndex->Delete(priority);
                avlIndex->Delete(priority);
            }
        }
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
        return out;
    }

    std::vector<int> TaskTree::GetTaskIdsWithPriorityRange(int lo, int hi) const {
        std::vector<int> priorities = CurrentTree()->InOrderRange(lo, hi);
        std::vector<int> result;

        for (int p : priorities) {
            auto it = tasksByPriority.find(p);
            if (it == tasksByPriority.end()) continue;
            for (int id : it->second) result.push_back(id);
        }
        return result;
    }

    std::vector<int> TaskTree::GetTaskIdsWithPriorityAtLeast(int minPriority) const {
        std::vector<int> priorities = CurrentTree()->GreaterOrEqual(minPriority);
        std::vector<int> result;

        for (int p : priorities) {
            auto it = tasksByPriority.find(p);
            if (it == tasksByPriority.end()) continue;
            for (int id : it->second) result.push_back(id);
        }
        return result;
    }

    std::vector<int> TaskTree::GetTopUrgentIncomplete(int n) const {
        // ќбход в пор€дке убывани€ приоритета: правые, корень, левые Ч но
        // у BST нет метода дл€ обратного обхода, поэтому берЄм InOrder и
        // разворачиваем.
        std::vector<int> priorities = CurrentTree()->InOrder();
        std::reverse(priorities.begin(), priorities.end());

        std::vector<int> result;
        for (int p : priorities) {
            auto it = tasksByPriority.find(p);
            if (it == tasksByPriority.end()) continue;

            for (int id : it->second) {
                auto ti = tasks.find(id);
                if (ti == tasks.end()) continue;
                if (ti->second.status == TaskStatus::Done) continue;
                result.push_back(id);
                if ((int)result.size() >= n) return result;
            }
        }
        return result;
    }

    void TaskTree::Clear() {
        tasks.clear();
        tasksByPriority.clear();
        bstIndex->Clear();
        avlIndex->Clear();
        nextId = 1;
    }

    void TaskTree::SetUseAvl(bool value) {
        useAvl = value;
    }

    std::vector<TaskTree::SnapshotNode> TaskTree::GetSnapshot() const {
        auto raw = CurrentTree()->GetSnapshot();
        std::vector<SnapshotNode> out;
        out.reserve(raw.size());
        for (auto& s : raw) {
            SnapshotNode sn;
            sn.priority = s.key;
            sn.parentIndex = s.parentIndex;
            sn.isLeft = s.isLeft;
            out.push_back(sn);
        }
        return out;
    }
}