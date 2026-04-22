import chromadb

client = chromadb.HttpClient(host='localhost', port=8000)
collection = client.get_collection(name='sales_data_collection')

# Query using Python client
results = collection.query(
    query_texts=['加油销售'],
    n_results=5,
    include=['documents', 'metadatas', 'distances']
)
print("Results:")
print(f"IDs: {results['ids']}")
print(f"Documents: {results['documents']}")
print(f"Distances: {results['distances']}")
print(f"Metadatas: {results['metadatas']}")