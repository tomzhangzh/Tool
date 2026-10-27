-- ---------------- 17. DynTemplateBlock 槽位关系（先删内置模板旧关系再重插，按 Code 关联 Id，幂等） ----------------

DELETE FROM DynTemplateBlock WHERE TemplateId IN (SELECT Id FROM DynTemplate WHERE Code IN ('triscreen-blocks','filterlist-crud','tree-detail','detail-modal',
'filter-list-open-windowV2','filter-list-drawer-leftV2','detail-modal-v2','tree-detail-v2'));
INSERT INTO DynTemplateBlock (TemplateId,Slot,BlockId,Required,SortNo)
SELECT (SELECT Id FROM DynTemplate WHERE Code='triscreen-blocks'),'filter',(SELECT Id FROM DynBlock WHERE Code='filter'),1,1
WHERE EXISTS (SELECT 1 FROM DynTemplate WHERE Code='triscreen-blocks') AND EXISTS (SELECT 1 FROM DynBlock WHERE Code='filter');
INSERT INTO DynTemplateBlock (TemplateId,Slot,BlockId,Required,SortNo)
SELECT (SELECT Id FROM DynTemplate WHERE Code='triscreen-blocks'),'list',(SELECT Id FROM DynBlock WHERE Code='list'),1,2
WHERE EXISTS (SELECT 1 FROM DynTemplate WHERE Code='triscreen-blocks') AND EXISTS (SELECT 1 FROM DynBlock WHERE Code='list');
INSERT INTO DynTemplateBlock (TemplateId,Slot,BlockId,Required,SortNo)
SELECT (SELECT Id FROM DynTemplate WHERE Code='triscreen-blocks'),'detail',(SELECT Id FROM DynBlock WHERE Code='detail'),1,3
WHERE EXISTS (SELECT 1 FROM DynTemplate WHERE Code='triscreen-blocks') AND EXISTS (SELECT 1 FROM DynBlock WHERE Code='detail');
INSERT INTO DynTemplateBlock (TemplateId,Slot,BlockId,Required,SortNo)
SELECT (SELECT Id FROM DynTemplate WHERE Code='filterlist-crud'),'filter',(SELECT Id FROM DynBlock WHERE Code='filter'),0,1
WHERE EXISTS (SELECT 1 FROM DynTemplate WHERE Code='filterlist-crud') AND EXISTS (SELECT 1 FROM DynBlock WHERE Code='filter');
INSERT INTO DynTemplateBlock (TemplateId,Slot,BlockId,Required,SortNo)
SELECT (SELECT Id FROM DynTemplate WHERE Code='filterlist-crud'),'list',(SELECT Id FROM DynBlock WHERE Code='list'),1,2
WHERE EXISTS (SELECT 1 FROM DynTemplate WHERE Code='filterlist-crud') AND EXISTS (SELECT 1 FROM DynBlock WHERE Code='list');
INSERT INTO DynTemplateBlock (TemplateId,Slot,BlockId,Required,SortNo)
SELECT (SELECT Id FROM DynTemplate WHERE Code='tree-detail'),'detail',(SELECT Id FROM DynBlock WHERE Code='detail'),1,1
WHERE EXISTS (SELECT 1 FROM DynTemplate WHERE Code='tree-detail') AND EXISTS (SELECT 1 FROM DynBlock WHERE Code='detail');
INSERT INTO DynTemplateBlock (TemplateId,Slot,BlockId,Required,SortNo)
SELECT (SELECT Id FROM DynTemplate WHERE Code='detail-modal'),'detail',(SELECT Id FROM DynBlock WHERE Code='detail'),1,1
WHERE EXISTS (SELECT 1 FROM DynTemplate WHERE Code='detail-modal') AND EXISTS (SELECT 1 FROM DynBlock WHERE Code='detail');
INSERT INTO DynTemplateBlock (TemplateId,Slot,BlockId,Required,SortNo)
SELECT (SELECT Id FROM DynTemplate WHERE Code='filter-list-open-windowV2'),'filter',(SELECT Id FROM DynBlock WHERE Code='filter'),1,1
WHERE EXISTS (SELECT 1 FROM DynTemplate WHERE Code='filter-list-open-windowV2') AND EXISTS (SELECT 1 FROM DynBlock WHERE Code='filter');
INSERT INTO DynTemplateBlock (TemplateId,Slot,BlockId,Required,SortNo)
SELECT (SELECT Id FROM DynTemplate WHERE Code='filter-list-open-windowV2'),'list',(SELECT Id FROM DynBlock WHERE Code='list'),1,2
WHERE EXISTS (SELECT 1 FROM DynTemplate WHERE Code='filter-list-open-windowV2') AND EXISTS (SELECT 1 FROM DynBlock WHERE Code='list');
INSERT INTO DynTemplateBlock (TemplateId,Slot,BlockId,Required,SortNo)
SELECT (SELECT Id FROM DynTemplate WHERE Code='filter-list-open-windowV2'),'detail',(SELECT Id FROM DynBlock WHERE Code='detail'),1,3
WHERE EXISTS (SELECT 1 FROM DynTemplate WHERE Code='filter-list-open-windowV2') AND EXISTS (SELECT 1 FROM DynBlock WHERE Code='detail');
INSERT INTO DynTemplateBlock (TemplateId,Slot,BlockId,Required,SortNo)
SELECT (SELECT Id FROM DynTemplate WHERE Code='filter-list-drawer-leftV2'),'filter',(SELECT Id FROM DynBlock WHERE Code='filter'),1,1
WHERE EXISTS (SELECT 1 FROM DynTemplate WHERE Code='filter-list-drawer-leftV2') AND EXISTS (SELECT 1 FROM DynBlock WHERE Code='filter');
INSERT INTO DynTemplateBlock (TemplateId,Slot,BlockId,Required,SortNo)
SELECT (SELECT Id FROM DynTemplate WHERE Code='filter-list-drawer-leftV2'),'list',(SELECT Id FROM DynBlock WHERE Code='list'),1,2
WHERE EXISTS (SELECT 1 FROM DynTemplate WHERE Code='filter-list-drawer-leftV2') AND EXISTS (SELECT 1 FROM DynBlock WHERE Code='list');
INSERT INTO DynTemplateBlock (TemplateId,Slot,BlockId,Required,SortNo)
SELECT (SELECT Id FROM DynTemplate WHERE Code='filter-list-drawer-leftV2'),'detail',(SELECT Id FROM DynBlock WHERE Code='detail'),1,3
WHERE EXISTS (SELECT 1 FROM DynTemplate WHERE Code='filter-list-drawer-leftV2') AND EXISTS (SELECT 1 FROM DynBlock WHERE Code='detail');
INSERT INTO DynTemplateBlock (TemplateId,Slot,BlockId,Required,SortNo)
SELECT (SELECT Id FROM DynTemplate WHERE Code='detail-modal-v2'),'detail',(SELECT Id FROM DynBlock WHERE Code='detail'),1,1
WHERE EXISTS (SELECT 1 FROM DynTemplate WHERE Code='detail-modal-v2') AND EXISTS (SELECT 1 FROM DynBlock WHERE Code='detail');
INSERT INTO DynTemplateBlock (TemplateId,Slot,BlockId,Required,SortNo)
SELECT (SELECT Id FROM DynTemplate WHERE Code='tree-detail-v2'),'tree',(SELECT Id FROM DynBlock WHERE Code='tree'),1,1
WHERE EXISTS (SELECT 1 FROM DynTemplate WHERE Code='tree-detail-v2') AND EXISTS (SELECT 1 FROM DynBlock WHERE Code='tree');
INSERT INTO DynTemplateBlock (TemplateId,Slot,BlockId,Required,SortNo)
SELECT (SELECT Id FROM DynTemplate WHERE Code='tree-detail-v2'),'detail',(SELECT Id FROM DynBlock WHERE Code='detail'),1,2
WHERE EXISTS (SELECT 1 FROM DynTemplate WHERE Code='tree-detail-v2') AND EXISTS (SELECT 1 FROM DynBlock WHERE Code='detail');