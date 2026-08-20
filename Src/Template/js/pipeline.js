// js/pipeline.js

let currentPage = 1;

async function loadPipelinePackages(page = 1) {
    try {
        // Simular llamada al endpoint / JSON
        const response = await fetch(`data/packages.json?page=${page}`);
        const data = await response.json();

        // 1. Actualizar Contadores del Toolbar
        const startRecord = (data.page - 1) * data.pageSize + 1;
        const endRecord = Math.min(data.page * data.pageSize, data.totalPackages);
        
        document.querySelector('.pipeline-counter').innerHTML = 
            `${startRecord}-${endRecord} of <strong>${data.totalPackages.toLocaleString()}</strong> packages`;

        // 2. Renderizar Contenido del Flow
        const flowContainer = document.querySelector('.node-flow-container');
        flowContainer.innerHTML = '';

        data.packages.forEach((pkg, index) => {
            const nodeHtml = createPackageNodeHTML(pkg);
            flowContainer.appendChild(nodeHtml);

            // Agregar conector entre nodos (excepto en el último)
            if (index < data.packages.length - 1) {
                const connector = document.createElement('div');
                connector.className = `flow-connector ${pkg.status === 'completed' ? 'active' : ''}`;
                flowContainer.appendChild(connector);
            }
        });

        // 3. Renderizar Paginación Dinámica
        renderPagination(data.page, data.totalPages);

        // 4. Re-inicializar Íconos de Lucide
        if (window.lucide) {
            lucide.createIcons();
        }

    } catch (error) {
        console.error("Error cargando los paquetes del pipeline:", error);
    }
}

// Función auxiliar para construir el HTML de una tarjeta
function createPackageNodeHTML(pkg) {
    const div = document.createElement('div');
    div.className = `flow-node node-${pkg.status}`;

    // Configuración de Badge y Botón de Reporte según el estado
    let statusBadge = '';
    let reportButton = '';

    if (pkg.status === 'completed') {
        statusBadge = `<span class="node-sub" style="color: #10B981; font-weight: 600;">✓ Completed</span>`;
        reportButton = `<button class="icon-button-sm" title="Ver reporte" onclick="openReport('${pkg.name}')"><i data-lucide="file-text"></i></button>`;
    } else if (pkg.status === 'running') {
        statusBadge = `<span class="node-sub" style="color: #2563EB; font-weight: 600;">↻ Running</span>`;
        reportButton = `<button class="icon-button-sm" title="Ver logs en vivo" onclick="openReport('${pkg.name}')"><i data-lucide="terminal"></i></button>`;
    } else if (pkg.status === 'failed') {
        statusBadge = `<span class="node-sub" style="color: #EF4444; font-weight: 600;">✕ Failed</span>`;
        reportButton = `<button class="icon-button-sm" title="Ver error log" onclick="openReport('${pkg.name}')"><i data-lucide="alert-triangle"></i></button>`;
    } else {
        statusBadge = `<span class="node-sub" style="color: #64748B; font-weight: 600;">⏱ Pending</span>`;
        reportButton = `<button class="icon-button-sm" disabled><i data-lucide="file-text"></i></button>`;
    }

    // Configuración del ícono del paquete
    const iconHeader = pkg.status === 'running' 
        ? `<i data-lucide="loader-2" class="spinner"></i>` 
        : (pkg.status === 'completed' ? `<i data-lucide="package-check"></i>` : `<i data-lucide="package"></i>`);

    // HTML de los Pasos (Steps)
    const stepsHTML = pkg.steps.map((step, idx) => {
        const isSpinner = step.status === 'active' ? 'spinner' : '';
        const stepCard = `
            <div class="step-card step-${step.status}">
                <i data-lucide="${step.icon}" class="step-card-icon ${isSpinner}"></i>
                <span class="step-card-name">${step.name}</span>
            </div>
        `;
        
        const isConnectorActive = (step.status === 'success' || step.status === 'active');
        const connectorLine = idx < pkg.steps.length - 1 
            ? `<div class="step-connector-line ${isConnectorActive ? 'active' : ''}"></div>` 
            : '';

        return stepCard + connectorLine;
    }).join('');

    div.innerHTML = `
        <div class="node-header">
            <div class="node-title">
                <div class="node-icon ${pkg.status}">
                    ${iconHeader}
                </div>
                <span class="node-name">${pkg.name}</span>
                <span class="node-sub">${pkg.duration}</span>
            </div>
            <div class="toolbar-section" style="gap: 6px;">
                ${reportButton}
                ${statusBadge}
            </div>
        </div>
        <div class="node-pipeline-steps">
            ${stepsHTML}
        </div>
    `;

    return div;
}

// Renderizado de la Paginación estilo Consola/Empresarial
function renderPagination(page, totalPages) {
    const paginationContainer = document.querySelector('.pagination');
    if (!paginationContainer) return;

    let pagesToDisplay = [];
    
    // Algoritmo para mostrar [1, 2, 3, 4, ..., 500]
    if (totalPages <= 5) {
        pagesToDisplay = Array.from({length: totalPages}, (_, i) => i + 1);
    } else {
        if (page <= 3) {
            pagesToDisplay = [1, 2, 3, 4, '...', totalPages];
        } else if (page >= totalPages - 2) {
            pagesToDisplay = [1, '...', totalPages - 3, totalPages - 2, totalPages - 1, totalPages];
        } else {
            pagesToDisplay = [1, '...', page - 1, page, page + 1, '...', totalPages];
        }
    }

    let html = `
        <button class="icon-button-sm" ${page === 1 ? 'disabled' : ''} onclick="changePage(${page - 1})">
            <i data-lucide="chevron-left"></i>
        </button>
        <div class="page-numbers">
    `;

    pagesToDisplay.forEach(p => {
        if (p === '...') {
            html += `<span class="page-dots">...</span>`;
        } else {
            html += `
                <button class="page-num ${p === page ? 'active' : ''}" onclick="changePage(${p})">
                    ${p}
                </button>
            `;
        }
    });

    html += `
        </div>
        <button class="icon-button-sm" ${page === totalPages ? 'disabled' : ''} onclick="changePage(${page + 1})">
            <i data-lucide="chevron-right"></i>
        </button>
    `;

    paginationContainer.innerHTML = html;
}

function changePage(newPage) {
    currentPage = newPage;
    loadPipelinePackages(newPage);
}

// Carga Inicial
document.addEventListener('DOMContentLoaded', () => {
    loadPipelinePackages(1);
});