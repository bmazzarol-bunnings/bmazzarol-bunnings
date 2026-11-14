-- Relationship hotspots: methods with high fan-in or fan-out within the
-- indexed graphs (the indexer's curated view).
WITH v AS (
  SELECT
    symbol_id,
    max(incoming_call_count) AS fan_in,
    max(outgoing_call_count) AS fan_out,
    max(relationship_score) AS relationship_score,
    first(display_name) AS display_name
  FROM method_relationship_hotspots
  GROUP BY symbol_id
)
SELECT
  v.relationship_score,
  v.fan_in,
  v.fan_out,
  v.display_name,
  min(d.path) AS path,
  min(sd.start_line) AS line
FROM v
JOIN symbols s ON s.symbol_id = v.symbol_id
JOIN symbol_declarations sd ON sd.symbol_id = s.symbol_id AND sd.is_primary
JOIN documents d ON d.document_id = sd.document_id
WHERE (d.path LIKE '%Components/Foundation/%'
       OR d.path LIKE '%Components/Pipelines/%'
       OR d.path LIKE '%Components/AI/%')
GROUP BY v.symbol_id, v.relationship_score, v.fan_in, v.fan_out, v.display_name
ORDER BY relationship_score DESC, path, line
LIMIT 50;
