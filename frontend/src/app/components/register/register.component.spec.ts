import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RegisterComponent } from './register.component';
import { AuthService } from '../../services/auth.service';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { ActivatedRoute } from '@angular/router';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  let authServiceSpy: any;
  let routerSpy: any;

  beforeEach(async () => {
    authServiceSpy = { register: vi.fn() };
    routerSpy = { navigate: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: {} },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should validate form fields', () => {
    component.email = '';
    component.password = '';
    component.confirmPassword = '';
    
    component.register();

    expect(component.errorMessage).toBe('All fields are required.');
    expect(authServiceSpy.register).not.toHaveBeenCalled();
  });

  it('should check if passwords match', () => {
    component.email = 'test@example.com';
    component.password = 'pass123';
    component.confirmPassword = 'pass456';
    
    component.register();

    expect(component.errorMessage).toBe('Passwords do not match.');
    expect(authServiceSpy.register).not.toHaveBeenCalled();
  });

  it('should reject weak passwords', () => {
    component.email = 'test@example.com';
    component.password = 'weak';
    component.confirmPassword = 'weak';
    
    component.register();

    expect(component.errorMessage).toBe('Password must be at least 8 characters and contain uppercase, lowercase, number, and special character.');
    expect(authServiceSpy.register).not.toHaveBeenCalled();
  });

  it('should call register on authService and navigate on success', () => {
    component.email = 'test@example.com';
    component.password = 'StrongPass123!';
    component.confirmPassword = 'StrongPass123!';
    component.preferredTopics = 'AI, Space';
    
    authServiceSpy.register.mockReturnValue(of({}));

    component.register();

    expect(authServiceSpy.register).toHaveBeenCalledWith('test@example.com', 'StrongPass123!', 'AI, Space');
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should display error when registration fails', () => {
    component.email = 'test@example.com';
    component.password = 'StrongPass123!';
    component.confirmPassword = 'StrongPass123!';
    
    authServiceSpy.register.mockReturnValue(throwError(() => ({ status: 409 })));

    component.register();

    expect(component.isLoading).toBe(false);
    expect(component.errorMessage).toBe('Account already exists.');
    expect(routerSpy.navigate).not.toHaveBeenCalled();
  });
});
