-- Branch-based cyclomatic proxy: 1 + branch statements (if/loop/switch
-- section/catch/goto) per method, from the statements table. Collapsed
-- across target frameworks (max).
WITH m AS (
  SELECT
    st.graph_id,
    st.method_symbol_id AS symbol_id,
    count(*) FILTER (
      WHERE st.kind IN ('IfStatement', 'ForStatement', 'WhileStatement',
                        'DoStatement', 'ForEachStatement',
                        'ForEachVariableStatement', 'SwitchSection',
                        'GotoStatement', 'CatchClause')
    ) AS branches
  FROM statements st
  JOIN documents d ON d.document_id = st.document_id
  WHERE d.path LIKE '%Components/Foundation/%'
     OR d.path LIKE '%Components/Pipelines/%'
     OR d.path LIKE '%Components/AI/%'
  GROUP BY st.graph_id, st.method_symbol_id
)
SELECT
  (max(branches) + 1) AS complexity,
  max(branches) AS branches,
  s.metadata_name,
  s.display_name,
  s.visibility,
  min(d.path) AS path,
  min(sd.start_line) AS line
FROM m
JOIN symbols s ON s.symbol_id = m.symbol_id
JOIN symbol_declarations sd ON sd.symbol_id = s.symbol_id AND sd.is_primary
JOIN documents d ON d.document_id = sd.document_id
GROUP BY s.symbol_id, s.metadata_name, s.display_name, s.visibility
HAVING max(branches) >= 8
ORDER BY complexity DESC, path, line
LIMIT 60;
