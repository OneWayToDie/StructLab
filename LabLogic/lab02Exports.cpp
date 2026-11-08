#include "TaskTree.h"
#include <cstring>
#include <string>

using namespace Lab02;

#pragma pack(push, 1)
struct CTask {
    int id;
    char title[200];
    char description[500];
    int priority;
    char dueDate[20];
    int status;
};

struct CSnapshotNode {
    int priority;
    int parentIndex;
    int isLeft;
};
#pragma pack(pop)

void CopyStr(char* dst, size_t cap, const std::string& src) {
    strncpy_s(dst, cap, src.c_str(), cap - 1);
}

void ConvertToC(const Task& t, CTask& ct) {
    ct.id = t.id;
    CopyStr(ct.title, sizeof(ct.title), t.title);
    CopyStr(ct.description, sizeof(ct.description), t.description);
    ct.priority = t.priority;
    CopyStr(ct.dueDate, sizeof(ct.dueDate), t.dueDate);
    ct.status = (int)t.status;
}

extern "C" {

    // ---------- ”œ–¿¬À≈Õ»≈ ----------
    __declspec(dllexport) void* Lab02_Create() {
        return new TaskTree();
    }

    __declspec(dllexport) void Lab02_Destroy(void* ptr) {
        if (ptr) delete static_cast<TaskTree*>(ptr);
    }

    __declspec(dllexport) void Lab02_Clear(void* ptr) {
        if (ptr) static_cast<TaskTree*>(ptr)->Clear();
    }

    // ---------- Œœ≈–¿÷»» ----------
    __declspec(dllexport) int Lab02_AddTask(void* ptr, const char* title,
        const char* description,
        int priority, const char* dueDate)
    {
        if (!ptr) return -1;
        return static_cast<TaskTree*>(ptr)->AddTask(title, description, priority, dueDate);
    }

    __declspec(dllexport) bool Lab02_DeleteTask(void* ptr, int taskId) {
        return ptr ? static_cast<TaskTree*>(ptr)->DeleteTask(taskId) : false;
    }

    __declspec(dllexport) bool Lab02_SetStatus(void* ptr, int taskId, int status) {
        return ptr ? static_cast<TaskTree*>(ptr)->SetStatus(taskId, (TaskStatus)status) : false;
    }

    // ---------- ◊“≈Õ»≈ ----------
    __declspec(dllexport) bool Lab02_GetTask(void* ptr, int taskId, CTask* out) {
        if (!ptr) return false;
        Task t;
        if (!static_cast<TaskTree*>(ptr)->GetTask(taskId, t)) return false;
        ConvertToC(t, *out);
        return true;
    }

    __declspec(dllexport) int Lab02_GetAllTasks(void* ptr, CTask* buffer, int maxSize) {
        if (!ptr) return 0;
        auto all = static_cast<TaskTree*>(ptr)->GetAllTasks();
        int n = (int)all.size();
        if (n > maxSize) n = maxSize;
        for (int i = 0; i < n; i++) ConvertToC(all[i], buffer[i]);
        return n;
    }

    __declspec(dllexport) int Lab02_GetTaskIdsInRange(void* ptr, int lo, int hi,
        int* buffer, int maxSize)
    {
        if (!ptr) return 0;
        auto ids = static_cast<TaskTree*>(ptr)->GetTaskIdsWithPriorityRange(lo, hi);
        int n = (int)ids.size();
        if (n > maxSize) n = maxSize;
        for (int i = 0; i < n; i++) buffer[i] = ids[i];
        return n;
    }

    __declspec(dllexport) int Lab02_GetTaskIdsAtLeast(void* ptr, int minPriority,
        int* buffer, int maxSize)
    {
        if (!ptr) return 0;
        auto ids = static_cast<TaskTree*>(ptr)->GetTaskIdsWithPriorityAtLeast(minPriority);
        int n = (int)ids.size();
        if (n > maxSize) n = maxSize;
        for (int i = 0; i < n; i++) buffer[i] = ids[i];
        return n;
    }

    __declspec(dllexport) int Lab02_GetTopUrgent(void* ptr, int n,
        int* buffer, int maxSize)
    {
        if (!ptr) return 0;
        auto ids = static_cast<TaskTree*>(ptr)->GetTopUrgentIncomplete(n);
        int m = (int)ids.size();
        if (m > maxSize) m = maxSize;
        for (int i = 0; i < m; i++) buffer[i] = ids[i];
        return m;
    }

    // ---------- ƒ≈–≈¬Œ ----------
    __declspec(dllexport) int Lab02_GetSnapshot(void* ptr, CSnapshotNode* buffer, int maxSize) {
        if (!ptr) return 0;
        auto snap = static_cast<TaskTree*>(ptr)->GetSnapshot();
        int n = (int)snap.size();
        if (n > maxSize) n = maxSize;
        for (int i = 0; i < n; i++) {
            buffer[i].priority = snap[i].priority;
            buffer[i].parentIndex = snap[i].parentIndex;
            buffer[i].isLeft = snap[i].isLeft;
        }
        return n;
    }

    __declspec(dllexport) int Lab02_GetTreeHeight(void* ptr) {
        return ptr ? static_cast<TaskTree*>(ptr)->GetTreeHeight() : 0;
    }

    __declspec(dllexport) int Lab02_GetIndexNodeCount(void* ptr) {
        return ptr ? static_cast<TaskTree*>(ptr)->GetIndexNodeCount() : 0;
    }

    __declspec(dllexport) const char* Lab02_GetTreeType(void* ptr) {
        return ptr ? static_cast<TaskTree*>(ptr)->GetTreeType() : "None";
    }

    __declspec(dllexport) bool Lab02_IsAvl(void* ptr) {
        return ptr ? static_cast<TaskTree*>(ptr)->IsAvl() : false;
    }

    __declspec(dllexport) void Lab02_SetUseAvl(void* ptr, bool value) {
        if (ptr) static_cast<TaskTree*>(ptr)->SetUseAvl(value);
    }
}