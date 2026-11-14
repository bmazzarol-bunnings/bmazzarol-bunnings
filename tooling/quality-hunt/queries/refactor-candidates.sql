-- Refactor candidates: the indexer's curated maintainability ranking
-- (statement density, nesting, branching, catch usage, capture pressure),
-- collapsed across target frameworks (max score).
WITH v AS (
  SELECT
    method_symbol_id,
    max(refactor_score) AS refactor_score,
    max(statement_count) AS statement_count,
    max(max_nesting_depth) AS max_nesting_depth,
    max(branch_count) AS branch_count,
    max(catch_clause_count) AS catch_clause_count,
    max(captured_region_count) AS captured_region_count,
    first(display_name) AS display_name,
    first(signature) AS signature
  FROM method_refactor_candidates
  GROUP BY method_symbol_id
)
SELECT
  v.refactor_score,
  v.statement_count,
  v.max_nesting_depth,
  v.branch_count,
  v.catch_clause_count,
  v.captured_region_count,
  v.display_name,
  min(d.path) AS path,
  min(sd.start_line) AS line
FROM v
JOIN symbols s ON s.symbol_id = v.method_symbol_id
JOIN symbol_declarations sd ON sd.symbol_id = s.symbol_id AND sd.is_primary
JOIN documents d ON d.document_id = sd.document_id
WHERE (d.path LIKE '%Components/Foundation/%'
       OR d.path LIKE '%Components/Pipelines/%'
       OR d.path LIKE '%Components/AI/%')
GROUP BY v.method_symbol_id, v.refactor_score, v.statement_count,
         v.max_nesting_depth, v.branch_count, v.catch_clause_count,
         v.captured_region_count, v.display_name
ORDER BY refactor_score DESC, path, line
LIMIT 50;
