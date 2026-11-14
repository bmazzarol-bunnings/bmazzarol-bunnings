-- Long methods: primary declaration span >= 40 lines, collapsed across
-- target frameworks (max span), wave components only.
WITH m AS (
  SELECT
    s.symbol_id, s.graph_id, s.metadata_name, s.display_name,
    s.visibility, s.containing_namespace, s.is_static,
    d.path, sd.start_line, sd.end_line,
    (sd.end_line - sd.start_line + 1) AS span
  FROM symbols s
  JOIN symbol_declarations sd ON sd.symbol_id = s.symbol_id AND sd.is_primary
  JOIN documents d ON d.document_id = sd.document_id
  WHERE s.kind = 'Method'
    AND (d.path LIKE '%Components/Foundation/%'
         OR d.path LIKE '%Components/Pipelines/%'
         OR d.path LIKE '%Components/AI/%')
)
SELECT
  max(span) AS span_lines,
  metadata_name,
  display_name,
  visibility,
  min(path) AS path,
  min(start_line) AS line
FROM m
GROUP BY metadata_name, display_name, visibility, path, start_line
HAVING max(span) >= 40
ORDER BY span_lines DESC, path, line
LIMIT 60;
