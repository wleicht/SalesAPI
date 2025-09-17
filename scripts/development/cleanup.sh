#!/bin/bash

# SalesAPI Cleanup Script
# Removes temporary files, build artifacts, and ensures clean workspace

echo "?? Iniciando limpeza do projeto SalesAPI..."

# Function to safely remove directories
safe_remove_dirs() {
    local pattern=$1
    local description=$2
    echo "  Removendo $description..."
    find . -name "$pattern" -type d -print0 | while IFS= read -r -d '' dir; do
        echo "    Removendo: $dir"
        rm -rf "$dir" 2>/dev/null || echo "    ??  Não foi possível remover: $dir"
    done
}

# Function to safely remove files
safe_remove_files() {
    local pattern=$1
    local description=$2
    echo "  Removendo $description..."
    find . -name "$pattern" -type f -print0 | while IFS= read -r -d '' file; do
        echo "    Removendo: $file"
        rm -f "$file" 2>/dev/null || echo "    ??  Não foi possível remover: $file"
    done
}

# Clean build artifacts
echo "?? Limpando artefatos de build..."
safe_remove_dirs "bin" "diretórios bin"
safe_remove_dirs "obj" "diretórios obj"

# Clean temporary files
echo "???  Limpando arquivos temporários..."
safe_remove_files "*.tmp" "arquivos temporários"
safe_remove_files "*.log" "arquivos de log"

# Clean IDE specific files
echo "?? Limpando arquivos específicos da IDE..."
safe_remove_dirs ".vs" "diretórios Visual Studio"
safe_remove_dirs ".vscode" "diretórios VS Code"
safe_remove_files "*.user" "arquivos de usuário"

# Clean test results
echo "?? Limpando resultados de testes..."
safe_remove_dirs "TestResults" "resultados de testes"
safe_remove_dirs "test-results" "resultados de testes"
if [ -d "scripts/manual-tests/results" ]; then
    echo "  Limpando resultados de testes manuais..."
    rm -rf scripts/manual-tests/results/* 2>/dev/null
    echo "    ? Resultados de testes manuais limpos"
fi

# Clean package caches (if any)
echo "?? Limpando caches de packages..."
safe_remove_dirs "packages" "diretórios packages"

# Clean any leftover Docker build contexts
echo "?? Limpando contextos Docker temporários..."
safe_remove_files ".dockerignore~*" "arquivos dockerignore temporários"

# Clean coverage reports
echo "?? Limpando relatórios de cobertura..."
safe_remove_dirs "coverage" "relatórios de cobertura"
safe_remove_files "*.coverage" "arquivos de cobertura"

# Show disk space saved
echo ""
echo "? Limpeza concluída com sucesso!"
echo ""
echo "?? Resumo da limpeza:"
echo "  • Artefatos de build removidos"
echo "  • Arquivos temporários removidos" 
echo "  • Caches da IDE limpos"
echo "  • Resultados de testes limpos"
echo "  • Contextos Docker temporários limpos"
echo ""
echo "?? O workspace está agora limpo e pronto para desenvolvimento!"