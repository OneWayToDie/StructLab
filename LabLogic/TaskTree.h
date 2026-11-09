#pragma once
#include <unordered_map>
#include <vector>
#include <string>
#include "Task.h"
#include "BST.h"
#include "AVLTree.h"

namespace Lab02 {

    class TaskTree {
    private:
        std::unordered_map<int, Task> tasks;
        BST* bstIndex;
        AVLTree* avlIndex;
        bool useAvl;
        int nextSequentialId;

        BST* CurrentTree() { return useAvl ? (BST*)avlIndex : bstIndex; }
        const BST* CurrentTree() const { return useAvl ? (const BST*)avlIndex : (const BST*)bstIndex; }

        int GenerateRandomId();

    public:
        TaskTree();
        ~TaskTree();

        int AddTask(const std::string& title, const std::string& desc,
            int priority, const std::string& dueDate, const std::string& dueTime,
            bool sequential, bool isManual);

        bool AddTaskWithId(int id, const std::string& title, const std::string& desc,
            int priority, const std::string& dueDate, const std::string& dueTime,
            TaskStatus status, bool isManual);

        bool DeleteTask(int taskId);
        bool SetStatus(int taskId, TaskStatus status);
        bool GetTask(int taskId, Task& out) const;
        std::vector<Task> GetAllTasks() const;

        std::vector<int> GetTaskIdsWithPriorityRange(int lo, int hi) const;
        std::vector<int> GetTaskIdsWithPriorityAtLeast(int minPriority) const;
        std::vector<int> GetTopUrgentIncomplete(int n) const;

        void Clear();

        bool IsAvl() const { return useAvl; }
        void SetUseAvl(bool value) { useAvl = value; }

        struct SnapshotNode {
            int key;
            int priority;
            int parentIndex;
            int isLeft;
        };
        std::vector<SnapshotNode> GetSnapshot() const;

        int GetTaskCount() const { return (int)tasks.size(); }
        int GetTreeHeight() const { return CurrentTree()->GetHeight(); }
        int GetIndexNodeCount() const { return CurrentTree()->GetCount(); }
        const char* GetTreeType() const { return CurrentTree()->GetTreeType(); }
    };
}