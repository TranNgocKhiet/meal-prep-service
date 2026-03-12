import { useState, useEffect, useRef } from 'react';
import { getErrorMessage } from '../types/errors';
import { useParams, useNavigate } from 'react-router-dom';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import './DeliveryTracking.css';

interface Location {
  latitude: number;
  longitude: number;
  timestamp: string;
}

interface DeliveryPersonnel {
  id: string;
  fullName: string;
  phoneNumber: string;
}

interface Delivery {
  id: string;
  orderId: string;
  orderNumber: string;
  status: string;
  deliveryAddress: string;
  currentLocation: Location | null;
  deliveryPersonnel: DeliveryPersonnel;
  assignedAt: string;
  deliveredAt: string | null;
  estimatedDeliveryTime: string | null;
}

const DeliveryTracking = () => {
  const { orderId } = useParams<{ orderId: string }>();
  const navigate = useNavigate();
  const [delivery, setDelivery] = useState<Delivery | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const mapRef = useRef<HTMLDivElement>(null);
  const googleMapRef = useRef<google.maps.Map | null>(null);
  const deliveryMarkerRef = useRef<google.maps.Marker | null>(null);
  const destinationMarkerRef = useRef<google.maps.Marker | null>(null);
  const routePolylineRef = useRef<google.maps.Polyline | null>(null);
  const updateIntervalRef = useRef<number | null>(null);

  useEffect(() => {
    loadGoogleMapsScript();
    return () => {
      if (updateIntervalRef.current) {
        clearInterval(updateIntervalRef.current);
      }
    };
  }, []);

  useEffect(() => {
    if (orderId) {
      fetchDelivery();
      // Set up 30-second interval for location updates
      updateIntervalRef.current = setInterval(() => {
        fetchDelivery();
      }, 30000);
    }

    return () => {
      if (updateIntervalRef.current) {
        clearInterval(updateIntervalRef.current);
      }
    };
  }, [orderId]);

  const loadGoogleMapsScript = () => {
    const apiKey = import.meta.env.VITE_GOOGLE_MAPS_API_KEY;
    if (!apiKey) {
      setError('Google Maps API key not configured');
      setLoading(false);
      return;
    }

    if (window.google && window.google.maps) {
      return;
    }

    const script = document.createElement('script');
    script.src = `https://maps.googleapis.com/maps/api/js?key=${apiKey}`;
    script.async = true;
    script.defer = true;
    document.head.appendChild(script);
  };

  const fetchDelivery = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get(`/delivery/order/${orderId}`);
      if (response.data.success) {
        const deliveryData = response.data.data;
        setDelivery(deliveryData);
        
        // Initialize or update map
        if (deliveryData.currentLocation) {
          initializeMap(deliveryData);
        }
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err) || 'Failed to load delivery information');
    } finally {
      setLoading(false);
    }
  };

  const initializeMap = (deliveryData: Delivery) => {
    if (!mapRef.current || !window.google || !window.google.maps) {
      return;
    }

    const currentLoc = deliveryData.currentLocation;
    if (!currentLoc) return;

    // Initialize map if not already done
    if (!googleMapRef.current) {
      googleMapRef.current = new google.maps.Map(mapRef.current, {
        zoom: 14,
        center: { lat: currentLoc.latitude, lng: currentLoc.longitude },
        mapTypeControl: false,
        streetViewControl: false,
      });
    }

    // Update or create delivery personnel marker
    if (deliveryMarkerRef.current) {
      deliveryMarkerRef.current.setPosition({
        lat: currentLoc.latitude,
        lng: currentLoc.longitude,
      });
    } else {
      deliveryMarkerRef.current = new google.maps.Marker({
        position: { lat: currentLoc.latitude, lng: currentLoc.longitude },
        map: googleMapRef.current,
        title: 'Delivery Personnel',
        icon: {
          url: 'data:image/svg+xml;charset=UTF-8,' + encodeURIComponent(`
            <svg xmlns="http://www.w3.org/2000/svg" width="40" height="40" viewBox="0 0 40 40">
              <circle cx="20" cy="20" r="18" fill="#4CAF50" stroke="white" stroke-width="3"/>
              <text x="20" y="27" font-size="20" text-anchor="middle" fill="white">🚚</text>
            </svg>
          `),
          scaledSize: new google.maps.Size(40, 40),
        },
      });
    }

    // Geocode destination address if not already done
    if (!destinationMarkerRef.current) {
      const geocoder = new google.maps.Geocoder();
      geocoder.geocode({ address: deliveryData.deliveryAddress }, (results, status) => {
        if (status === 'OK' && results && results[0]) {
          const destLocation = results[0].geometry.location;
          
          destinationMarkerRef.current = new google.maps.Marker({
            position: destLocation,
            map: googleMapRef.current!,
            title: 'Delivery Address',
            icon: {
              url: 'data:image/svg+xml;charset=UTF-8,' + encodeURIComponent(`
                <svg xmlns="http://www.w3.org/2000/svg" width="40" height="40" viewBox="0 0 40 40">
                  <path d="M20 5 C13 5 8 10 8 17 C8 26 20 35 20 35 C20 35 32 26 32 17 C32 10 27 5 20 5 Z" fill="#FF5722" stroke="white" stroke-width="2"/>
                  <circle cx="20" cy="17" r="5" fill="white"/>
                </svg>
              `),
              scaledSize: new google.maps.Size(40, 40),
            },
          });

          // Draw route
          drawRoute(currentLoc, destLocation);

          // Fit bounds to show both markers
          const bounds = new google.maps.LatLngBounds();
          bounds.extend({ lat: currentLoc.latitude, lng: currentLoc.longitude });
          bounds.extend(destLocation);
          googleMapRef.current?.fitBounds(bounds);
        }
      });
    } else {
      // Update route if destination already exists
      const destPos = destinationMarkerRef.current.getPosition();
      if (destPos) {
        drawRoute(currentLoc, destPos);
      }
    }
  };

  const drawRoute = (start: Location | { lat: number; lng: number }, end: google.maps.LatLng) => {
    if (!googleMapRef.current) return;

    const startLatLng = 'latitude' in start 
      ? { lat: start.latitude, lng: start.longitude }
      : start;

    // Remove old route
    if (routePolylineRef.current) {
      routePolylineRef.current.setMap(null);
    }

    // Draw new route
    const directionsService = new google.maps.DirectionsService();
    directionsService.route(
      {
        origin: startLatLng,
        destination: end,
        travelMode: google.maps.TravelMode.DRIVING,
      },
      (result, status) => {
        if (status === 'OK' && result) {
          routePolylineRef.current = new google.maps.Polyline({
            path: result.routes[0].overview_path,
            geodesic: true,
            strokeColor: '#4CAF50',
            strokeOpacity: 0.8,
            strokeWeight: 4,
            map: googleMapRef.current!,
          });
        }
      }
    );
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'assigned':
        return 'status-assigned';
      case 'pickedup':
        return 'status-pickedup';
      case 'intransit':
        return 'status-intransit';
      case 'delivered':
        return 'status-delivered';
      case 'failed':
        return 'status-failed';
      default:
        return '';
    }
  };

  const formatEstimatedTime = (timeString: string | null) => {
    if (!timeString) return 'Calculating...';
    
    // Parse timespan format (HH:mm:ss)
    const parts = timeString.split(':');
    if (parts.length === 3) {
      const hours = parseInt(parts[0]);
      const minutes = parseInt(parts[1]);
      
      if (hours > 0) {
        return `${hours}h ${minutes}m`;
      }
      return `${minutes} minutes`;
    }
    return timeString;
  };

  if (loading && !delivery) {
    return (
      <Container>
        <div className="loading-container">
          <div className="spinner"></div>
          <p>Loading delivery information...</p>
        </div>
      </Container>
    );
  }

  if (error && !delivery) {
    return (
      <Container>
        <div className="error-container">
          <div className="error-icon">⚠️</div>
          <h2>Unable to Load Delivery</h2>
          <p>{error}</p>
          <button className="btn btn-primary" onClick={() => navigate('/orders')}>
            Back to Orders
          </button>
        </div>
      </Container>
    );
  }

  if (!delivery) {
    return (
      <Container>
        <div className="empty-state">
          <div className="empty-icon">📦</div>
          <h2>No Delivery Information</h2>
          <p>This order doesn't have delivery tracking yet.</p>
          <button className="btn btn-primary" onClick={() => navigate('/orders')}>
            Back to Orders
          </button>
        </div>
      </Container>
    );
  }

  return (
    <Container>
      <div className="delivery-tracking-page">
        <div className="page-header">
          <button className="btn-back" onClick={() => navigate(`/orders/${orderId}`)}>
            ← Back to Order
          </button>
          <h1>Track Delivery</h1>
        </div>

        <div className="tracking-container">
          <div className="map-container">
            <div ref={mapRef} className="map" />
            {!delivery.currentLocation && (
              <div className="map-overlay">
                <p>Waiting for delivery personnel location...</p>
              </div>
            )}
          </div>

          <div className="delivery-info">
            <div className="info-card">
              <h2>Order #{delivery.orderNumber}</h2>
              <div className="status-badge-large">
                <span className={`status-badge ${getStatusColor(delivery.status)}`}>
                  {delivery.status}
                </span>
              </div>
            </div>

            <div className="info-card">
              <h3>Delivery Status</h3>
              <div className="status-timeline">
                <div className={`timeline-item ${['assigned', 'pickedup', 'intransit', 'delivered'].includes(delivery.status.toLowerCase()) ? 'completed' : ''}`}>
                  <div className="timeline-dot"></div>
                  <div className="timeline-content">
                    <p className="timeline-title">Assigned</p>
                    <p className="timeline-time">
                      {new Date(delivery.assignedAt).toLocaleString()}
                    </p>
                  </div>
                </div>
                <div className={`timeline-item ${['pickedup', 'intransit', 'delivered'].includes(delivery.status.toLowerCase()) ? 'completed' : ''}`}>
                  <div className="timeline-dot"></div>
                  <div className="timeline-content">
                    <p className="timeline-title">Picked Up</p>
                  </div>
                </div>
                <div className={`timeline-item ${['intransit', 'delivered'].includes(delivery.status.toLowerCase()) ? 'completed' : ''}`}>
                  <div className="timeline-dot"></div>
                  <div className="timeline-content">
                    <p className="timeline-title">In Transit</p>
                  </div>
                </div>
                <div className={`timeline-item ${delivery.status.toLowerCase() === 'delivered' ? 'completed' : ''}`}>
                  <div className="timeline-dot"></div>
                  <div className="timeline-content">
                    <p className="timeline-title">Delivered</p>
                    {delivery.deliveredAt && (
                      <p className="timeline-time">
                        {new Date(delivery.deliveredAt).toLocaleString()}
                      </p>
                    )}
                  </div>
                </div>
              </div>
            </div>

            <div className="info-card">
              <h3>Delivery Details</h3>
              <div className="detail-row">
                <span className="detail-label">Delivery Address:</span>
                <span className="detail-value">{delivery.deliveryAddress}</span>
              </div>
              <div className="detail-row">
                <span className="detail-label">Estimated Time:</span>
                <span className="detail-value">
                  {formatEstimatedTime(delivery.estimatedDeliveryTime)}
                </span>
              </div>
              {delivery.currentLocation && (
                <div className="detail-row">
                  <span className="detail-label">Last Updated:</span>
                  <span className="detail-value">
                    {new Date(delivery.currentLocation.timestamp).toLocaleTimeString()}
                  </span>
                </div>
              )}
            </div>

            <div className="info-card">
              <h3>Delivery Personnel</h3>
              <div className="personnel-info">
                <div className="personnel-avatar">
                  {delivery.deliveryPersonnel.fullName.charAt(0)}
                </div>
                <div>
                  <p className="personnel-name">{delivery.deliveryPersonnel.fullName}</p>
                  <p className="personnel-phone">{delivery.deliveryPersonnel.phoneNumber}</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Container>
  );
};

export default DeliveryTracking;
