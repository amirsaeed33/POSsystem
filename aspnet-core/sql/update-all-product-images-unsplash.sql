/*
  Assign Unsplash food photos to every AppProducts row that has no ImagePath.
  Safe to re-run: only updates NULL / empty ImagePath.
*/
SET NOCOUNT ON;

DECLARE @Q NVARCHAR(80) = N'?auto=format&fit=crop&w=800&h=600&q=80';

;WITH Targets AS (
    SELECT
        p.Id,
        LOWER(CONCAT(ISNULL(p.Name, N''), N' ', ISNULL(c.Name, N''))) AS Haystack
    FROM dbo.AppProducts p
    LEFT JOIN dbo.AppCategories c ON c.Id = p.CategoryId
    WHERE p.IsDeleted = 0
      AND (p.ImagePath IS NULL OR LTRIM(RTRIM(p.ImagePath)) = N'')
)
UPDATE p
SET p.ImagePath =
    CASE
        WHEN t.Haystack LIKE N'%burger%' OR t.Haystack LIKE N'%burgur%'
            THEN N'https://images.unsplash.com/photo-1568901346375-23c9450c58cd' + @Q
        WHEN t.Haystack LIKE N'%pizza%'
            THEN N'https://images.unsplash.com/photo-1513104890138-7c749659a591' + @Q
        WHEN t.Haystack LIKE N'%biryani%'
            THEN N'https://images.unsplash.com/photo-1563379926898-05f4575a45d8' + @Q
        WHEN t.Haystack LIKE N'%karahi%' OR t.Haystack LIKE N'%handi%'
            THEN N'https://images.unsplash.com/photo-1603894584373-5ac82b2ae398' + @Q
        WHEN t.Haystack LIKE N'%bbq%' OR t.Haystack LIKE N'%tikka%' OR t.Haystack LIKE N'%kabab%' OR t.Haystack LIKE N'%kebab%' OR t.Haystack LIKE N'%boti%'
            THEN N'https://images.unsplash.com/photo-1599487488170-d11ec9c172f2' + @Q
        WHEN t.Haystack LIKE N'%naan%' OR t.Haystack LIKE N'%roti%' OR t.Haystack LIKE N'%paratha%' OR t.Haystack LIKE N'%bread%' OR t.Haystack LIKE N'%bun%'
            THEN N'https://images.unsplash.com/photo-1601050690597-df0568f70950' + @Q
        WHEN t.Haystack LIKE N'%cake%' OR t.Haystack LIKE N'%pastry%' OR t.Haystack LIKE N'%donut%' OR t.Haystack LIKE N'%muffin%'
            THEN N'https://images.unsplash.com/photo-1578985545062-69928b1d9587' + @Q
        WHEN t.Haystack LIKE N'%cookie%' OR t.Haystack LIKE N'%biscuit%' OR t.Haystack LIKE N'%rusk%'
            THEN N'https://images.unsplash.com/photo-1499636136210-6f4ee915583e' + @Q
        WHEN t.Haystack LIKE N'%fries%' OR t.Haystack LIKE N'%fry%'
            THEN N'https://images.unsplash.com/photo-1573080496219-bb080dd4f877' + @Q
        WHEN t.Haystack LIKE N'%salad%' OR t.Haystack LIKE N'%raita%'
            THEN N'https://images.unsplash.com/photo-1512621776951-a57141f2eefd' + @Q
        WHEN t.Haystack LIKE N'%drink%' OR t.Haystack LIKE N'%juice%' OR t.Haystack LIKE N'%shake%' OR t.Haystack LIKE N'%lassi%' OR t.Haystack LIKE N'%tea%' OR t.Haystack LIKE N'%coffee%'
            THEN N'https://images.unsplash.com/photo-1544145945-f90425340c7e' + @Q
        WHEN t.Haystack LIKE N'%milk%' OR t.Haystack LIKE N'%dairy%' OR t.Haystack LIKE N'%butter%' OR t.Haystack LIKE N'%ghee%'
            THEN N'https://images.unsplash.com/photo-1563636619-e9143da7973b' + @Q
        WHEN t.Haystack LIKE N'%chicken%'
            THEN N'https://images.unsplash.com/photo-1626645738196-c2a7c87a8f58' + @Q
        WHEN t.Haystack LIKE N'%fish%' OR t.Haystack LIKE N'%prawn%'
            THEN N'https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2' + @Q
        WHEN t.Haystack LIKE N'%rice%' OR t.Haystack LIKE N'%pulao%'
            THEN N'https://images.unsplash.com/photo-1516684669134-de6f7c473a2a' + @Q
        WHEN t.Haystack LIKE N'%snack%' OR t.Haystack LIKE N'%chips%'
            THEN N'https://images.unsplash.com/photo-1566478989037-eec1753df085' + @Q
        WHEN t.Haystack LIKE N'%grocery%' OR t.Haystack LIKE N'%flour%' OR t.Haystack LIKE N'%oil%' OR t.Haystack LIKE N'%spice%'
            THEN N'https://images.unsplash.com/photo-1542838132-92c53300491e' + @Q
        ELSE N'https://images.unsplash.com/photo-1555939594-58d7cb561ad1' + @Q
    END
FROM dbo.AppProducts p
INNER JOIN Targets t ON t.Id = p.Id;

SELECT COUNT(*) AS ProductsUpdated
FROM dbo.AppProducts
WHERE IsDeleted = 0
  AND ImagePath LIKE N'https://images.unsplash.com/%';
