WITH cte AS (
    SELECT 
	    id,
		pid,
		name, department,
		CAST(0 AS varbinary(max)) AS Level
    FROM orgChart
    WHERE pid = 229091257
  UNION ALL
  SELECT 
    i.id,
    i.pid, 
	i.name, i.department,
    Level + CAST(i.id AS varbinary(max)) AS Level
    FROM orgChart i
  INNER JOIN cte cc
    ON cc.id = i.pid
)
SELECT c.department
      ,c.name
      ,h.date
      ,h.enter
      ,h.eexit
      ,h.eventscount
      ,h.totalworktime
      ,h.totalouttime
from (SELECT pid, id, department, name FROM cte 
) c 
inner join Parsec2ExcelHistory h on h.id = c.id and h.date_ >= '01-05-2019' and h.date_ <= '25-05-2019'
order by c.pid asc, c.name asc, h.date_ asc