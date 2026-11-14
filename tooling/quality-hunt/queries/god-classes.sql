-- God classes: types with the most declared methods in wave components,
-- collapsed across target frameworks (max count).
WITH c AS (
  SELECT
    t.symbol_id, t.graph_id, t.metadata_name, t.display_name, t.visibility,
    count(s.symbol_id) AS method_count
  FROM symbols t
  JOIN symbols s ON s.containing_symbol_id = t.symbol_id AND s.kind = 'Method'
  JOIN documents d ON d.document_id = t.document_id
  WHERE t.kind = 'NamedType'
    AND (d.path LIKE '%Components/Foundation/%'
         OR d.path LIKE '%Components/Pipelines/%'
         OR d.path LIKE '%Components/AI/%')
  GROUP BY t.symbol_id, t.graph_id, t.metadata_name, t.display_name, t.visibility
)
SELECT
  max(method_count) AS method_count,
  metadata_name,
  display_name,
  visibility,
  min(d.path) AS path
FROM c
JOIN symbol_declarations sd ON sd.symbol_id = c.symbol_id AND sd.is_primary
JOIN documents d ON d.document_id = sd.document_id
GROUP BY c.metadata_name, c.display_name, c.visibility
ORDER BY method_count DESC, path, metadata_name
LIMIT 40;
