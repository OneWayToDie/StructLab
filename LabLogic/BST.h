#pragma once
#include <vector>

namespace Lab02 {

    // Бинарное дерево поиска по int (приоритет задачи).
    // AVLTree наследует этот класс и переопределяет Insert/Delete.
    class BST {
    protected:
        struct Node {
            int key;
            Node* left;
            Node* right;
            Node* parent;
            int height;   // для AVL; для BST не критично, но пусть будет
            Node(int k) : key(k), left(nullptr), right(nullptr), parent(nullptr), height(1) {}
        };

        Node* root;
        int count;

        // Виртуальные — AVL переопределит
        virtual Node* InsertRec(Node* node, int key);
        virtual Node* DeleteRec(Node* node, int key);

        // Ротации (используются только в AVL, но лежат здесь для переиспользования)
        Node* RotateLeft(Node* x);
        Node* RotateRight(Node* y);

        void DestroyRec(Node* n);
        int HeightRec(Node* n) const;
        Node* FindMin(Node* n) const;
        Node* FindMax(Node* n) const;

        void InOrderRec(Node* n, std::vector<int>& out) const;
        void PreOrderRec(Node* n, std::vector<int>& out) const;
        void PostOrderRec(Node* n, std::vector<int>& out) const;

        void InOrderRangeRec(Node* n, int lo, int hi, std::vector<int>& out) const;
        void GreaterOrEqualRec(Node* n, int key, std::vector<int>& out) const;

    public:
        BST();
        virtual ~BST();

        virtual void Insert(int key);
        virtual bool Delete(int key);
        bool Contains(int key) const;

        int GetCount() const { return count; }
        int GetHeight() const;
        int GetMin() const;   // INT_MIN если пусто
        int GetMax() const;   // INT_MAX если пусто

        void Clear();

        // Обходы (возвращают последовательности ключей)
        std::vector<int> InOrder() const;
        std::vector<int> PreOrder() const;
        std::vector<int> PostOrder() const;

        // Диапазонный поиск
        std::vector<int> InOrderRange(int lo, int hi) const;
        std::vector<int> GreaterOrEqual(int key) const;

        // Снимок для визуализации: (key, parentIndex, isLeft)
        // parentIndex = -1 для корня, isLeft: -1 (корень), 0 (правый), 1 (левый)
        struct SnapshotNode {
            int key;
            int parentIndex;
            int isLeft;
        };
        std::vector<SnapshotNode> GetSnapshot() const;

        // Виртуально — AVL переопределит обход (порядок тот же, но может понадобиться)
        virtual const char* GetTreeType() const { return "BST"; }
    };
}