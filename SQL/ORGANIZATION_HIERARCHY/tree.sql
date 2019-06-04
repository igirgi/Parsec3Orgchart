  WITH cte AS (
    SELECT org_id, parent_id, direct_parent
	,		CAST(0 AS varbinary(max)) AS Level
	from [Parsec3].[dbo].[ORGANIZATION_HIERARCHY]
	  where PARENT_ID = 'DBAFE6D5-6108-4130-BF4D-B7557252315C' and direct_parent = 1
  UNION ALL
    SELECT i.org_id, i.parent_id, i.direct_parent
	,		Level + CAST(i.org_id AS varbinary(max)) AS Level
	from [Parsec3].[dbo].[ORGANIZATION_HIERARCHY] i
  INNER JOIN cte cc
    ON cc.org_id = i.parent_id
)
SELECT org_id, parent_id, direct_parent FROM cte 