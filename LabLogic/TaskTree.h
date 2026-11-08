#pragma once
#include <unordered_map>
#include <vector>
#include "Task.h"
#include "BST.h"
#include "AVLTree.h"

namespace Lab02 {

    class TaskTree {
    private:
        // Хранилище задач по ID
        std::unordered_map<int, Task> tasks;

        // Индекс: Priority -> список ID задач с этим приоритетом
        std::unordered_map<int, std::vector<int>> tasksByPriority;

        // Дерево приоритетов (ключ — Priority)
        BST* bstIndex;
        AVLTree* avlIndex;
        bool useAvl;

        int nextId;

        BST* CurrentTree() { return useAvl ? (BST*)avlIndex : bstIndex; }
        const BST* CurrentTree() const { return useAvl ? (const BST*)avlIndex : (const BST*)bstIndex; }

        void SyncIndexes();   // вставить все текущие приоритеты в оба индекса

    public:
        TaskTree();
        ~TaskTree();

        int AddTask(const std::string& title, const std::string& desc,
            int priority, const std::string& dueDate);
        bool DeleteTask(int taskId);
        bool SetStatus(int taskId, TaskStatus status);

        bool GetTask(int taskId, Task& out) const;
        std::vector<Task> GetAllTasks() const;

        // Задачи по диапазону приоритетов (возвращает ID)
        std::vector<int> GetTaskIdsWithPriorityRange(int lo, int hi) const;

        // Задачи с приоритетом >= N (для "Что делать сегодня?")
        std::vector<int> GetTaskIdsWithPriorityAtLeast(int minPriority) const;

        // Топ-N самых срочных невыполненных
        std::vector<int> GetTopUrgentIncomplete(int n) const;

        void Clear();

        // Переключение BST / AVL
        bool IsAvl() const { return useAvl; }
        void SetUseAvl(bool value);

        // Снимок текущего дерева для визуализации
        struct SnapshotNode {
            int priority;
            int parentIndex;
            int isLeft;
        };
        std::vector<SnapshotNode> GetSnapshot() const;

        // Мета
        int GetTaskCount() const { return (int)tasks.size(); }
        int GetTreeHeight() const { return CurrentTree()->GetHeight(); }
        int GetIndexNodeCount() const { return CurrentTree()->GetCount(); }
        const char* GetTreeType() const { return CurrentTree()->GetTreeType(); }
    };
}
