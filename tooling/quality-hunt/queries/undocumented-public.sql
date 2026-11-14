-- Public methods without XML doc comments (the indexer's curated view),
-- wave components only, for the readability pool class.
SELECT
  v.display_name,
  v.document_path AS path
FROM public_methods_without_docs v
WHERE (v.document_path LIKE '%Components/Foundation/%'
       OR v.document_path LIKE '%Components/Pipelines/%'
       OR v.document_path LIKE '%Components/AI/%')
GROUP BY v.symbol_id, v.display_name, v.document_path
ORDER BY path, v.display_name
LIMIT 100;
