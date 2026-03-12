import type { ReactNode } from 'react';
import './Container.css';

interface ContainerProps {
  children: ReactNode;
  className?: string;
  maxWidth?: 'sm' | 'md' | 'lg' | 'xl' | 'full';
}

const Container = ({ children, className = '', maxWidth = 'xl' }: ContainerProps) => {
  return (
    <div className={`app-container app-container--${maxWidth} ${className}`}>
      {children}
    </div>
  );
};

export default Container;
