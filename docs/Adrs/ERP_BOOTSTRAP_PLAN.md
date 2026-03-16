ERP — Plano de Bootstrap (Composição sobre a Foundation)
Status: Draft
Data: 2026-03-16
Base: ADR-002, ADR-005

## Objetivo
Descrever a composição inicial do ERP como primeiro produto consumidor da foundation, sem alterar a estrutura atual.

## Módulos iniciais
- Finance
- Sales
- Inventory
- Purchasing

## Ordem de registro sugerida (bootstrap)
1. Registrar módulos (`IProductComposition.RegisterModules`).
2. Registrar permissões canônicas `{module}.{resource}.{action}` no `IPermissionCatalog`.
3. Registrar features por módulo no `IFeatureCatalog`.
4. Registrar endpoints por módulo.
5. Registrar jobs/background tasks (se existirem).

## Rascunho de permissões canônicas
- Finance: `finance.invoice.read`, `finance.invoice.create`, `finance.invoice.update`, `finance.invoice.approve`.
- Sales: `sales.order.read`, `sales.order.create`, `sales.order.update`, `sales.order.cancel`.
- Inventory: `inventory.item.read`, `inventory.item.manage`, `inventory.stock.read`, `inventory.stock.adjust`.
- Purchasing: `purchasing.po.read`, `purchasing.po.create`, `purchasing.po.approve`, `purchasing.po.receive`.

## Rascunho de features (exemplos)
- Finance: `finance.invoice.notifications`, `finance.invoice.export`.
- Sales: `sales.order.discounts`, `sales.order.workflows`.
- Inventory: `inventory.stock.alerts`, `inventory.stock.replenishment`.
- Purchasing: `purchasing.po.three-way-match`, `purchasing.vendor.portal`.

## Próximos passos
- Completar lista de permissões/features com PO/negócio e mapear para policies.
- Definir quais features são habilitáveis por tenant e defaults por tenant.
- Atualizar `docs/security/RBAC_MATRIX.md` quando endpoints forem definidos.
