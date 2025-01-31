import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConfirmEmailPage } from './confirm-email.page';

describe('ConfirmEmailPage', () => {
  let component: ConfirmEmailPage;
  let fixture: ComponentFixture<ConfirmEmailPage>;

  beforeEach(() => {
    fixture = TestBed.createComponent(ConfirmEmailPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
