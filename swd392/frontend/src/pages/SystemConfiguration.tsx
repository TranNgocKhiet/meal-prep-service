import { useState, useEffect } from 'react';
import apiClient from '../config/api';
import './AdminCrud.css';

interface SystemConfig {
  id: string;
  key: string;
  value: string;
  dataType: string;
  description: string;
  updatedAt: string;
}

const SystemConfiguration = () => {
  const [configs, setConfigs] = useState<SystemConfig[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [editingConfig, setEditingConfig] = useState<SystemConfig | null>(null);
  const [editValue, setEditValue] = useState('');

  const defaultConfigs = [
    { key: 'MaxDatesInMealPlan', value: '7', dataType: 'int', description: 'Max number of dates in a meal plan' },
    { key: 'MaxRecipesInMeal', value: '10', dataType: 'int', description: 'Max number of recipes in a meal' },
    { key: 'MaxMealPlansPerAccount', value: '5', dataType: 'int', description: 'Max number of meal plans per account (without subscription)' },
    { key: 'MaxFridgeItemsPerAccount', value: '100', dataType: 'int', description: 'Max fridge items per account (without subscription)' },
    { key: 'MaxDeliveryDistance', value: '10', dataType: 'decimal', description: 'Max delivery distance in km' }
  ];

  useEffect(() => {
    fetchConfigs();
  }, []);

  const fetchConfigs = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get('/systemconfigurations');
      
      if (response.data.success) {
        const existingConfigs = response.data.data;
        
        // Check if default configs exist, if not create them
        const existingKeys = existingConfigs.map((c: SystemConfig) => c.key);
        const missingConfigs = defaultConfigs.filter(dc => !existingKeys.includes(dc.key));
        
        if (missingConfigs.length > 0) {
          // Create missing configs
          for (const config of missingConfigs) {
            await apiClient.post('/systemconfigurations', config);
          }
          // Refetch after creating
          const refetchResponse = await apiClient.get('/systemconfigurations');
          setConfigs(refetchResponse.data.data);
        } else {
          setConfigs(existingConfigs);
        }
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load system configurations');
    } finally {
      setLoading(false);
    }
  };

  const handleEdit = (config: SystemConfig) => {
    setEditingConfig(config);
    setEditValue(config.value);
  };

  const handleSave = async () => {
    if (!editingConfig) return;

    try {
      await apiClient.put(`/systemconfigurations/${editingConfig.id}`, {
        ...editingConfig,
        value: editValue
      });
      setEditingConfig(null);
      fetchConfigs();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to update configuration');
    }
  };

  const handleCancel = () => {
    setEditingConfig(null);
    setEditValue('');
  };

  if (loading) {
    return <div className="container"><div className="loading">Loading...</div></div>;
  }

  return (
    <div className="container">
      <div className="crud-header">
        <h1 style={{ color: '#fff' }}>System Configuration</h1>
      </div>

      {error && <div className="error-message">{error}</div>}

      <div className="crud-table-container">
        <table className="crud-table">
          <thead>
            <tr>
              <th>Configuration</th>
              <th>Value</th>
              <th>Type</th>
              <th>Last Updated</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {configs.map((config) => (
              <tr key={config.id}>
                <td>
                  <div>
                    <strong>{config.key}</strong>
                    <div style={{ fontSize: '0.875rem', color: '#718096', marginTop: '0.25rem' }}>
                      {config.description}
                    </div>
                  </div>
                </td>
                <td>
                  {editingConfig?.id === config.id ? (
                    <input
                      type={config.dataType === 'int' || config.dataType === 'decimal' ? 'number' : 'text'}
                      value={editValue}
                      onChange={(e) => setEditValue(e.target.value)}
                      style={{ padding: '0.5rem', border: '1px solid #e2e8f0', borderRadius: '4px' }}
                    />
                  ) : (
                    <strong>{config.value}</strong>
                  )}
                </td>
                <td>{config.dataType}</td>
                <td>{new Date(config.updatedAt).toLocaleDateString()}</td>
                <td>
                  {editingConfig?.id === config.id ? (
                    <>
                      <button onClick={handleSave} className="btn-edit" style={{ marginRight: '0.5rem' }}>Save</button>
                      <button onClick={handleCancel} className="btn-secondary">Cancel</button>
                    </>
                  ) : (
                    <button onClick={() => handleEdit(config)} className="btn-edit">Edit</button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default SystemConfiguration;
