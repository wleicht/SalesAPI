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
        rm -rf "$dir" 2>/dev/null || echo "    ??  N�o foi poss�vel remover: $dir"
    done
}

# Function to safely remove files
safe_remove_files() {
    local pattern=$1
    local description=$2
    echo "  Removendo $description..."
    find . -name "$pattern" -type f -print0 | while IFS= read -r -d '' file; do
        echo "    Removendo: $file"
        rm -f "$file" 2>/dev/null || echo "    ??  N�o foi poss�vel remover: $file"
    done
}

# Clean build artifacts
echo "?? Limpando artefatos de build..."
safe_remove_dirs "bin" "diret�rios bin"
safe_remove_dirs "obj" "diret�rios obj"

# Clean temporary files
echo "???  Limpando arquivos tempor�rios..."
safe_remove_files "*.tmp" "arquivos tempor�rios"
safe_remove_files "*.log" "arquivos de log"

# Clean IDE specific files
echo "?? Limpando arquivos espec�ficos da IDE..."
safe_remove_dirs ".vs" "diret�rios Visual Studio"
safe_remove_dirs ".vscode" "diret�rios VS Code"
safe_remove_files "*.user" "arquivos de usu�rio"

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
safe_remove_dirs "packages" "diret�rios packages"

# Clean any leftover Docker build contexts
echo "?? Limpando contextos Docker tempor�rios..."
safe_remove_files ".dockerignore~*" "arquivos dockerignore tempor�rios"

# Clean coverage reports
echo "?? Limpando relat�rios de cobertura..."
safe_remove_dirs "coverage" "relat�rios de cobertura"
safe_remove_files "*.coverage" "arquivos de cobertura"

# Show disk space saved
echo ""
echo "? Limpeza conclu�da com sucesso!"
echo ""
echo "?? Resumo da limpeza:"
echo "  � Artefatos de build removidos"
echo "  � Arquivos tempor�rios removidos" 
echo "  � Caches da IDE limpos"
echo "  � Resultados de testes limpos"
echo "  � Contextos Docker tempor�rios limpos"
echo ""
echo "?? O workspace est� agora limpo e pronto para desenvolvimento!"