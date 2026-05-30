export default async function handler(req, res) {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type, X-ELS-APIKey');

  if (req.method === 'OPTIONS') {
    return res.status(200).end();
  }

  const { scopusId, view = "META_ABS" } = req.query;
  if (!scopusId) {
    return res.status(400).json({ error: "Missing scopusId parameter" });
  }
  
  const apiKey = "d529415ff014b69d7aa600ed720df15e";
  const url = `https://api.elsevier.com/content/abstract/scopus_id/${scopusId}?view=${view}`;
  
  try {
    const scopusRes = await fetch(url, {
      headers: {
        "X-ELS-APIKey": apiKey,
        "Accept": "application/json"
      }
    });
    
    if (!scopusRes.ok) {
      return res.status(scopusRes.status).send(await scopusRes.text());
    }
    
    const data = await scopusRes.json();
    return res.status(200).json(data);
  } catch (error) {
    return res.status(500).json({ error: error.message });
  }
}
