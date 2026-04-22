import chromadb
import httpx

# Monkey patch to log requests
original_send = httpx.Client.send
def logging_send(self, request, **kwargs):
    print(f"Request: {request.method} {request.url}")
    print(f"Headers: {dict(request.headers)}")
    if request.content:
        print(f"Body: {request.content.decode('utf-8')[:200]}...")
    response = original_send(self, request, **kwargs)
    print(f"Response: {response.status_code}")
    return response

httpx.Client.send = logging_send

client = chromadb.HttpClient(host='localhost', port=8000)
print("=== Listing collections ===")
collections = client.list_collections()
print(f"Collections: {collections}")

print("\n=== Creating collection ===")
collection = client.create_collection(name='test_collection_2')
print(f"Collection: {collection}")

print("\n=== Adding document ===")
collection.add(
    documents=["测试数据"],
    metadatas=[{"type": "test"}],
    ids=["test_id"]
)

print("\n=== Querying ===")
results = collection.query(
    query_texts=["查询测试"],
    n_results=1
)
print(f"Results: {results}")