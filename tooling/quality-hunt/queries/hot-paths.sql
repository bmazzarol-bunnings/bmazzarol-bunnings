-- Hot paths by call count: total invocation edges per callee method, summed
-- across all indexed graphs (callers anywhere in the wave). Callee must be
-- a wave-component method.
WITH fanin AS (
  SELECT
    sc.metadata_name,
    count(*) AS call_count
  FROM call_edges ce
  JOIN symbols sc ON sc.symbol_id = ce.callee_symbol_id
  JOIN documents cd ON cd.document_id = ce.document_id
  WHERE sc.kind = 'Method'
    AND (cd.path LIKE '%Components/Foundation/%'
         OR cd.path LIKE '%Components/Pipelines/%'
         OR cd.path LIKE '%Components/AI/%')
  GROUP BY sc.metadata_name
)
SELECT
  f.call_count,
  f.metadata_name,
  s.display_name,
  s.visibility,
  min(d.path) AS path,
  min(sd.start_line) AS line
FROM fanin f
JOIN symbols s ON s.metadata_name = f.metadata_name AND s.kind = 'Method'
JOIN symbol_declarations sd ON sd.symbol_id = s.symbol_id AND sd.is_primary
JOIN documents d ON d.document_id = sd.document_id
WHERE (d.path LIKE '%Components/Foundation/%'
       OR d.path LIKE '%Components/Pipelines/%'
       OR d.path LIKE '%Components/AI/%')
GROUP BY f.metadata_name, f.call_count, s.display_name, s.visibility
ORDER BY call_count DESC, path, line
LIMIT 60;
