#include "BST.h"
#include <algorithm>
#include <climits>
#include <functional>

namespace Lab02 {

    BST::BST() : root(nullptr), count(0) {}
    BST::~BST() { DestroyRec(root); }

    void BST::DestroyRec(Node* n) {
        if (!n) return;
        DestroyRec(n->left);
        DestroyRec(n->right);
        delete n;
    }

    void BST::Insert(int key) {
        root = InsertRec(root, key);
    }

    BST::Node* BST::InsertRec(Node* node, int key) {
        if (!node) {
            count++;
            return new Node(key);
        }
        if (key < node->key) {
            Node* l = InsertRec(node->left, key);
            l->parent = node;
            node->left = l;
        }
        else if (key > node->key) {
            Node* r = InsertRec(node->right, key);
            r->parent = node;
            node->right = r;
        }
        return node;
    }

    bool BST::Delete(int key) {
        int before = count;
        root = DeleteRec(root, key);
        return count < before;
    }

    BST::Node* BST::DeleteRec(Node* node, int key) {
        if (!node) return nullptr;

        if (key < node->key) {
            node->left = DeleteRec(node->left, key);
            if (node->left) node->left->parent = node;
        }
        else if (key > node->key) {
            node->right = DeleteRec(node->right, key);
            if (node->right) node->right->parent = node;
        }
        else {
            // Нашли
            if (!node->left && !node->right) {
                delete node;
                count--;
                return nullptr;
            }
            if (!node->left) {
                Node* r = node->right;
                r->parent = node->parent;
                delete node;
                count--;
                return r;
            }
            if (!node->right) {
                Node* l = node->left;
                l->parent = node->parent;
                delete node;
                count--;
                return l;
            }
            // Два ребёнка
            Node* succ = FindMin(node->right);
            node->key = succ->key;
            node->right = DeleteRec(node->right, succ->key);
            if (node->right) node->right->parent = node;
        }
        return node;
    }

    bool BST::Contains(int key) const {
        Node* c = root;
        while (c) {
            if (key == c->key) return true;
            c = key < c->key ? c->left : c->right;
        }
        return false;
    }

    BST::Node* BST::FindMin(Node* n) const {
        while (n && n->left) n = n->left;
        return n;
    }

    BST::Node* BST::FindMax(Node* n) const {
        while (n && n->right) n = n->right;
        return n;
    }

    int BST::GetMin() const {
        Node* m = FindMin(root);
        return m ? m->key : INT_MIN;
    }

    int BST::GetMax() const {
        Node* m = FindMax(root);
        return m ? m->key : INT_MAX;
    }

    int BST::HeightRec(Node* n) const {
        if (!n) return 0;
        return 1 + std::max(HeightRec(n->left), HeightRec(n->right));
    }

    int BST::GetHeight() const { return HeightRec(root); }

    void BST::Clear() {
        DestroyRec(root);
        root = nullptr;
        count = 0;
    }

    void BST::InOrderRec(Node* n, std::vector<int>& out) const {
        if (!n) return;
        InOrderRec(n->left, out);
        out.push_back(n->key);
        InOrderRec(n->right, out);
    }

    void BST::PreOrderRec(Node* n, std::vector<int>& out) const {
        if (!n) return;
        out.push_back(n->key);
        PreOrderRec(n->left, out);
        PreOrderRec(n->right, out);
    }

    void BST::PostOrderRec(Node* n, std::vector<int>& out) const {
        if (!n) return;
        PostOrderRec(n->left, out);
        PostOrderRec(n->right, out);
        out.push_back(n->key);
    }

    std::vector<int> BST::InOrder() const {
        std::vector<int> out;
        InOrderRec(root, out);
        return out;
    }

    std::vector<int> BST::PreOrder() const {
        std::vector<int> out;
        PreOrderRec(root, out);
        return out;
    }

    std::vector<int> BST::PostOrder() const {
        std::vector<int> out;
        PostOrderRec(root, out);
        return out;
    }

    void BST::InOrderRangeRec(Node* n, int lo, int hi, std::vector<int>& out) const {
        if (!n) return;
        if (lo < n->key) InOrderRangeRec(n->left, lo, hi, out);
        if (n->key >= lo && n->key <= hi) out.push_back(n->key);
        if (hi > n->key) InOrderRangeRec(n->right, lo, hi, out);
    }

    std::vector<int> BST::InOrderRange(int lo, int hi) const {
        std::vector<int> out;
        InOrderRangeRec(root, lo, hi, out);
        return out;
    }

    void BST::GreaterOrEqualRec(Node* n, int key, std::vector<int>& out) const {
        if (!n) return;
        if (n->key >= key) {
            GreaterOrEqualRec(n->left, key, out);
            out.push_back(n->key);
        }
        GreaterOrEqualRec(n->right, key, out);
    }

    std::vector<int> BST::GreaterOrEqual(int key) const {
        std::vector<int> out;
        GreaterOrEqualRec(root, key, out);
        return out;
    }

    // --- Ротации (понадобятся AVL) ---
    BST::Node* BST::RotateLeft(Node* x) {
        Node* y = x->right;
        if (!y) return x;

        x->right = y->left;
        if (y->left) y->left->parent = x;

        y->parent = x->parent;
        y->left = x;
        x->parent = y;

        return y;
    }

    BST::Node* BST::RotateRight(Node* y) {
        Node* x = y->left;
        if (!x) return y;

        y->left = x->right;
        if (x->right) x->right->parent = y;

        x->parent = y->parent;
        x->right = y;
        y->parent = x;

        return x;
    }

    // --- Снимок для визуализации ---
    std::vector<BST::SnapshotNode> BST::GetSnapshot() const {
        std::vector<SnapshotNode> out;

        std::function<void(Node*, int)> rec = [&](Node* n, int parentIdx) {
            if (!n) return;
            int currentIdx = (int)out.size();

            int isLeft = -1;
            if (parentIdx >= 0) {
                // Найдём родителя в out, чтобы понять, левый или правый мы ребёнок
                // Проще: parent->left == this
                // Но out[parentIdx] не хранит Node*. Хранимую структуру придётся запомнить.
                // Альтернатива: вставлять информацию при рекурсивном вызове.
            }

            SnapshotNode sn;
            sn.key = n->key;
            sn.parentIndex = parentIdx;
            sn.isLeft = isLeft;
            out.push_back(sn);

            if (n->left) {
                out[currentIdx].isLeft = 1;
                rec(n->left, currentIdx);
            }
            if (n->right) {
                if (n->left == nullptr) out[currentIdx].isLeft = 0;
                rec(n->right, currentIdx);
            }
            // Уточним isLeft у родителя: если это первый ребёнок — 1, если второй — 0
            // Точнее: isLeft у ребёнка говорит, левый он или правый. Для этого
            // нужно в момент добавления знать, куда идём.
            // Перепишем: перед rec передадим, какой это ребёнок.
            };

        // Перепишем аккуратнее:
        std::function<void(Node*, int, int)> rec2 = [&](Node* n, int parentIdx, int isLeftFlag) {
            if (!n) return;
            int currentIdx = (int)out.size();

            SnapshotNode sn;
            sn.key = n->key;
            sn.parentIndex = parentIdx;
            sn.isLeft = parentIdx == -1 ? -1 : isLeftFlag;
            out.push_back(sn);

            rec2(n->left, currentIdx, 1);
            rec2(n->right, currentIdx, 0);
            };

        rec2(root, -1, -1);
        return out;
    }
}