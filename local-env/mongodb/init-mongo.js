// MongoDB initialization script for KafkaFlow Contrib
print('Initializing MongoDB for KafkaFlow...');

// Switch to the kafkaflow_test database
db = db.getSiblingDB('kafkaflow_test');

// Create a user for the kafkaflow_test database with readWrite permissions
db.createUser({
  user: 'kafkaflow',
  pwd: 'kafkaflow123',
  roles: [
    {
      role: 'readWrite',
      db: 'kafkaflow_test'
    }
  ]
});

// Create collections if they don't exist (optional - MongoDB creates them automatically)
db.createCollection('outbox');
db.createCollection('process_states');

print('MongoDB initialization completed.');