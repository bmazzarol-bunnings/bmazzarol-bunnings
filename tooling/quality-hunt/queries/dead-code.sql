-- Dead code (in-scope): private methods with zero call edges as callee and
-- zero symbol references anywhere in the index. Caveat: references via
-- delegates/attributes/DI are not modelled, so verify before acting.
WITH refs AS (
  SELECT DISTINCT to_symbol_id FROM symbol_references
  UNION
  SELECT DISTINCT callee_symbol_id FROM call_edges
)
SELECT
  s.metadata_name,
  s.display_name,
  s.visibility,
  d.path,
  sd.start_line AS line
FROM symbols s
JOIN symbol_declarations sd ON sd.symbol_id = s.symbol_id AND sd.is_primary
JOIN documents d ON d.document_id = sd.document_id
LEFT JOIN refs r ON r.to_symbol_id = s.symbol_id
LEFT JOIN accessors acc ON acc.symbol_id = s.symbol_id AND acc.graph_id = s.graph_id
WHERE s.kind = 'Method'
  AND s.visibility = 'Private'
  AND s.is_implicitly_declared = false
  AND acc.symbol_id IS NULL  -- accessors: refs hit the Property symbol, not the accessor
  AND r.to_symbol_id IS NULL
  AND (d.path LIKE '%Components/Foundation/%'
       OR d.path LIKE '%Components/Pipelines/%'
       OR d.path LIKE '%Components/AI/%')
ORDER BY d.path, sd.start_line
LIMIT 200;
