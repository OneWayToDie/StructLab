#include "AVLTree.h"
#include <algorithm>

namespace Lab02 {

    int AVLTree::Height(Node* n) const {
        return n ? n->height : 0;
    }

    int AVLTree::Balance(Node* n) const {
        return n ? Height(n->left) - Height(n->right) : 0;
    }

    void AVLTree::UpdateHeight(Node* n) {
        if (!n) return;
        n->height = 1 + std::max(Height(n->left), Height(n->right));
    }

    BST::Node* AVLTree::BalanceNode(Node* n) {
        if (!n) return n;
        UpdateHeight(n);
        int bal = Balance(n);

        // Левый перевес
        if (bal > 1) {
            if (Balance(n->left) < 0)
                n->left = RotateLeft(n->left);
            return RotateRight(n);
        }
        // Правый перевес
        if (bal < -1) {
            if (Balance(n->right) > 0)
                n->right = RotateRight(n->right);
            return RotateLeft(n);
        }
        return n;
    }

    BST::Node* AVLTree::InsertRec(Node* node, int key) {
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
        else {
            return node; // дубликат — не вставляем
        }
        return BalanceNode(node);
    }

    BST::Node* AVLTree::DeleteRec(Node* node, int key) {
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
            if (!node->left || !node->right) {
                Node* child = node->left ? node->left : node->right;
                if (child) child->parent = node->parent;
                delete node;
                count--;
                return child;
            }
            // Два ребёнка
            Node* succ = FindMin(node->right);
            node->key = succ->key;
            node->right = DeleteRec(node->right, succ->key);
            if (node->right) node->right->parent = node;
        }
        return BalanceNode(node);
    }
}