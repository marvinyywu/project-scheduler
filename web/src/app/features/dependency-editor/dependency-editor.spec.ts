import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { DependencyEditor } from './dependency-editor';

describe('DependencyEditor', () => {
  let component: DependencyEditor;
  let fixture: ComponentFixture<DependencyEditor>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DependencyEditor],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(DependencyEditor);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
