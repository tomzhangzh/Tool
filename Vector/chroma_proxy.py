from fastapi import FastAPI, HTTPException
import chromadb
from typing import List, Dict, Any
from pydantic import BaseModel

app = FastAPI()

client = chromadb.HttpClient(host='localhost', port=8000)

class AddRequest(BaseModel):
    documents: List[str]
    metadatas: List[Dict[str, str]] = None

class QueryRequest(BaseModel):
    query_texts: List[str]
    n_results: int = 5

@app.post("/api/collection/{collection_name}/add")
async def add_documents(collection_name: str, request: AddRequest):
    try:
        collection = client.get_or_create_collection(name=collection_name)
        current_count = collection.count()
        ids = [str(i + current_count) for i in range(len(request.documents))]
        
        if request.metadatas is None:
            request.metadatas = [{}] * len(request.documents)
        
        collection.add(
            documents=request.documents,
            metadatas=request.metadatas,
            ids=ids
        )
        return {"status": "success", "ids": ids}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/api/collection/{collection_name}/query")
async def query_documents(collection_name: str, request: QueryRequest):
    try:
        collection = client.get_collection(name=collection_name)
        results = collection.query(
            query_texts=request.query_texts,
            n_results=request.n_results,
            include=['documents', 'metadatas', 'distances']
        )
        return results
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/api/collection/{collection_name}/count")
async def get_count(collection_name: str):
    try:
        collection = client.get_collection(name=collection_name)
        count = collection.count()
        return {"count": count}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/api/collections")
async def list_collections():
    try:
        collections = client.list_collections()
        return [{"name": c.name} for c in collections]
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="localhost", port=8001)