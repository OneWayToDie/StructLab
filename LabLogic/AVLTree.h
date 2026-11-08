#pragma once
#include "BST.h"

namespace Lab02 {

    class AVLTree : public BST {
    protected:
        int Height(Node* n) const;
        int Balance(Node* n) const;
        void UpdateHeight(Node* n);
        Node* BalanceNode(Node* n);

        Node* InsertRec(Node* node, int key) override;
        Node* DeleteRec(Node* node, int key) override;

    public:
        AVLTree() = default;
        ~AVLTree() override = default;

        const char* GetTreeType() const override { return "AVL"; }
    };
}
